# 第 11 章 线程与线程池 🟡

> 本章练习：[04-thread-start.cs](../advanced/04-thread-start.cs)、[04-thread-parameterized.cs](../advanced/04-thread-parameterized.cs)、[04-threadpool-threads.cs](../advanced/04-threadpool-threads.cs)、[04-threadpool-queue-work-item.cs](../advanced/04-threadpool-queue-work-item.cs)、[04-manual-reset-event.cs](../advanced/04-manual-reset-event.cs)、[04-execution-context-asynclocal.cs](../advanced/04-execution-context-asynclocal.cs)

> 现代 C# 写并发，日常用的是 `Task` 和 `async/await`（第 12、13 章），很少直接 `new Thread`。但 Task 底下跑的就是线程池线程，不懂线程和线程池，下面这些问题就解释不了：
> 程序跑完了 `Main`，进程却不退出；`QueueUserWorkItem` 扔进去的任务一行都没输出；服务一到高峰就变慢，CPU 却不高（线程池饥饿）；子线程里抛了个异常，整个进程直接崩了；从 Java 带来的 `ThreadLocal` 习惯，到了 `await` 之后值就没了。
> 本章讲：怎么创建线程、传参、等待；前台线程和后台线程；线程池的工作方式和饥饿；用信号（`ManualResetEvent`、`CountdownEvent`）等待其他线程；执行上下文和 `AsyncLocal`。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果（本机 CPU 8 核）。多线程的输出顺序每次运行可能不同。

## 1. 创建线程：Thread 🟡

### 1.1 new Thread / Start / Join

和 Java 几乎一样（[04-thread-start.cs](../advanced/04-thread-start.cs)）：

```csharp
static void Main(string[] args)
{
    Thread thread = new Thread(new ThreadStart(Run));   // 也可以简写成 new Thread(Run)
    thread.Start();
    Console.WriteLine("main thread");                   // 通常比子线程先打印，但顺序不保证

    thread.Join();                                      // 等这个线程执行完再往下走
    Console.WriteLine("main thread end");
}

static void Run()
{
    Thread.Sleep(500);
    Console.WriteLine($"sub thread, IsBackground = {Thread.CurrentThread.IsBackground}");
}
```
```
main thread
sub thread, IsBackground = False
main thread end
```

| Java | C# |
|---|---|
| `new Thread(runnable)` | `new Thread(ThreadStart)`，直接传方法名或 lambda |
| `t.start()` | `t.Start()` |
| `t.join()` / `t.join(ms)` | `t.Join()` / `t.Join(ms)`（返回 `bool`：是否在时间内结束） |
| `Thread.sleep(ms)` | `Thread.Sleep(ms)` |
| `Thread.currentThread()` | `Thread.CurrentThread` |
| `t.setDaemon(true)` | `t.IsBackground = true` |
| `t.setName("x")` | `t.Name = "x"` |
| `extends Thread` 重写 `run()` | ❌ `Thread` 是 `sealed`，不能继承，只能传委托 |

看一下不同来源的线程有什么区别：

```csharp
static void Main(string[] args)
{
    Show("Main");
    var t = new Thread(() => Show("new Thread")) { Name = "worker-1" };
    t.Start(); t.Join();
    using var done = new ManualResetEventSlim(false);
    ThreadPool.QueueUserWorkItem(_ => { Show("QueueUserWorkItem"); done.Set(); });
    done.Wait();
}
static void Show(string label)
{
    var c = Thread.CurrentThread;
    Console.WriteLine($"{label,-18} id={c.ManagedThreadId} name={c.Name ?? "null"} pool={c.IsThreadPoolThread} bg={c.IsBackground}");
}
```
```
Main               id=1 name=null pool=False bg=False
new Thread         id=4 name=worker-1 pool=False bg=False
QueueUserWorkItem  id=5 name=.NET TP Worker pool=True bg=True
```

- `new Thread` 每次**新建**一个专用线程，默认是前台线程，用完就销毁。
- 线程池线程（`.NET TP Worker`）是**后台线程**，用完还回池里复用。
- 线程 id 用 `Environment.CurrentManagedThreadId` 或 `Thread.CurrentThread.ManagedThreadId`，每次运行可能不同。

### 1.2 给线程传参数

两种写法（[04-thread-parameterized.cs](../advanced/04-thread-parameterized.cs)）：

```csharp
// 老写法：ParameterizedThreadStart，只能传一个 object? 参数
Thread thread = new Thread(new ParameterizedThreadStart(Run));
thread.Start(DateTime.Now);
thread.Join();

// 现代写法：lambda 捕获变量，类型安全，不需要转换
var now = DateTime.Now;
var thread2 = new Thread(() => Console.WriteLine($"lambda: {now:HH:mm:ss}"));
thread2.Start();
thread2.Join();

// 委托定义是 ParameterizedThreadStart(object? obj)，参数要写 object?（写 object 会警告 CS8622）
static void Run(object? param)
{
    if (param is DateTime time)
        Console.WriteLine($"sub thread: {time:HH:mm:ss}");
}
```
```
sub thread: 17:30:46
lambda: 17:30:46
```

| | `ParameterizedThreadStart` + `Start(arg)` | lambda 捕获 |
|---|---|---|
| 参数个数 | 只能 1 个 `object?` | 任意多个 |
| 类型 | 要自己转换（`is` / 强转） | 编译时检查 |
| 推荐 | 看懂老代码就行 | ✅ |

⚠️ lambda 捕获的是**变量**，不是值。在 `for` 循环里捕获循环变量 `i` 时，线程真正执行时 `i` 可能已经变了。在循环体里先复制一份 `int id = i;` 再捕获（`foreach` 的循环变量每轮是新的，没这个问题）。Java 要求捕获的变量 effectively final，C# 没有这个限制，所以更要小心。

## 2. 前台线程 vs 后台线程 🟡

**只要还有前台线程在运行，进程就不会退出**；进程退出时，后台线程直接被终止，不会执行完，也不会执行 `finally`。

实测：`Main` 启动一个线程后直接返回，子线程 1 秒后打印：

```csharp
static void Main(string[] args)
{
    bool background = args.Length > 0 && args[0] == "bg";
    var thread = new Thread(() =>
    {
        Thread.Sleep(1000);
        Console.WriteLine("子线程跑完了");
    });
    thread.IsBackground = background;
    thread.Start();
    Console.WriteLine($"Main 结束（IsBackground = {background}）");
}
```

`dotnet run fgbg.cs`（前台线程）：

```
Main 结束（IsBackground = False）
子线程跑完了
```

`dotnet run fgbg.cs -- bg`（后台线程）：

```
Main 结束（IsBackground = True）
```

| | 前台线程 | 后台线程 |
|---|---|---|
| 谁是 | `new Thread` 默认 | 设置了 `IsBackground = true`；**线程池线程全都是**（`Task.Run`、`QueueUserWorkItem`、定时器回调） |
| 进程退出时 | 进程等它跑完 | 直接被杀掉 |
| Java 对应 | 普通线程 | daemon 线程 |
| 典型坑 | 忘了停的前台线程让进程一直不退出 | `Main` 没等，任务还没执行进程就退出了 |

- 第二个坑非常常见：`Main` 里 `Task.Run(...)` 或 `QueueUserWorkItem(...)` 之后什么都不等，程序直接退出、一行输出都没有。要用 `await`、`Join`、`CountdownEvent` 等方式等它们完成。
- 第 14 章的 volatile 实验就利用了前台线程：子线程死循环，进程就一直不退出。

## 3. 线程池 🟡

### 3.1 为什么要线程池

`new Thread` 每次都向操作系统申请线程，开销大（每个线程默认预留约 1MB 栈空间），用完就销毁。线程池先准备好一批线程，任务来了排队，空闲线程取任务执行，执行完不销毁，接着取下一个。

Java 里对应 `ExecutorService`（`Executors.newFixedThreadPool`），但需要自己创建和关闭；**.NET 每个进程自带一个全局线程池**，不用创建，也不用关闭。`Task.Run`、`Parallel.For`、`await` 之后的代码、定时器回调，都跑在这个线程池上。

### 3.2 两类线程与线程数

```csharp
ThreadPool.GetMinThreads(out int minWorker, out int minIo);          // 可以"立即"创建的线程数
ThreadPool.GetMaxThreads(out int maxWorker, out int maxIo);          // 上限
ThreadPool.GetAvailableThreads(out int availWorker, out int availIo); // 上限 - 正在使用的

Console.WriteLine($"CPU 核数: {Environment.ProcessorCount}");
Console.WriteLine($"最小值: workerThreads={minWorker}, completionPortThreads={minIo}");
Console.WriteLine($"最大值: workerThreads={maxWorker}, completionPortThreads={maxIo}");
Console.WriteLine($"可用数: workerThreads={availWorker}, completionPortThreads={availIo}");
```
```
CPU 核数: 8
最小值: workerThreads=8, completionPortThreads=1
最大值: workerThreads=32767, completionPortThreads=1000
可用数: workerThreads=32767, completionPortThreads=1000
```

| 线程类型 | 干什么 | 最小值 |
|---|---|---|
| 工作线程（worker） | 执行计算任务：`Task.Run`、`QueueUserWorkItem`、`Parallel.For`、`await` 之后的代码 | 默认 = CPU 核数 |
| I/O 完成端口线程（completion port） | 处理异步 I/O 完成后的回调，来自 Windows 的 IOCP | Mac/Linux 上基本用不到 |

- **最小线程数**不是"一开始就创建这么多"，而是"需要时可以**立即**创建到这么多"。程序刚启动时线程池里一个线程都没有，按需创建。
- 超过最小值之后，线程池**不会立即加线程**，而是大约**每 0.5 秒**才加一个（防止线程太多反而互相抢 CPU）。
- 最大值 32767 基本等于没有上限，真正限制你的是上面的注入速度。

### 3.3 线程池饥饿 ⚠️

如果在线程池线程里**阻塞**（`Thread.Sleep`、`.Result`、`.Wait()`、同步 I/O），线程被占着不干活，新任务只能排队，线程池又只能每 0.5 秒加一个线程，整个程序就会变慢——这叫**线程池饥饿**。

实测：30 个任务，每个阻塞 2 秒：

```csharp
var sw = Stopwatch.StartNew();
var tasks = new List<Task>();
for (int i = 0; i < 30; i++)
    tasks.Add(Task.Run(() => Thread.Sleep(2000)));   // 阻塞线程池线程

for (int i = 0; i < 6; i++)
{
    Console.WriteLine($"{sw.ElapsedMilliseconds,5}ms  ThreadCount = {ThreadPool.ThreadCount}, 排队 = {ThreadPool.PendingWorkItemCount}");
    Thread.Sleep(500);
}
Task.WaitAll(tasks.ToArray());
Console.WriteLine($"30 个任务全部完成，用时 {sw.ElapsedMilliseconds}ms");
```
```
CPU 核数 = 8, 最小工作线程 = 8
    1ms  ThreadCount = 8, 排队 = 22
  506ms  ThreadCount = 9, 排队 = 21
 1009ms  ThreadCount = 10, 排队 = 20
 1514ms  ThreadCount = 11, 排队 = 19
 2018ms  ThreadCount = 11, 排队 = 11
 2521ms  ThreadCount = 11, 排队 = 10
30 个任务全部完成，用时 6004ms
```

从输出能看到：

1. 一开始立即创建了 8 个线程（= 最小值 = CPU 核数），剩下 22 个任务排队。
2. 之后每 0.5 秒左右才加 1 个线程：8 → 9 → 10 → 11。
3. 2 秒后第一批任务完成，线程空出来，就不再加线程了。
4. 30 个任务如果同时执行只要 2 秒，实际用了 6 秒。

在 Web 服务器上，这就表现为"高峰时请求越来越慢，CPU 却不高"。解决办法：

| 做法 | 说明 |
|---|---|
| ✅ 用 `async/await` 代替阻塞 | `await Task.Delay` 代替 `Thread.Sleep`，`await` 代替 `.Result`，等待期间线程还回池里（第 13 章） |
| ✅ 长时间运行的任务不要占线程池 | 用 `new Thread` 或 `Task.Factory.StartNew(..., TaskCreationOptions.LongRunning)` |
| ⚠️ 调大最小线程数 | `ThreadPool.SetMinThreads(...)`，治标不治本 |

### 3.4 QueueUserWorkItem

最原始的线程池用法：把一个委托放进队列，立即返回（[04-threadpool-queue-work-item.cs](../advanced/04-threadpool-queue-work-item.cs)）：

```csharp
using var done = new CountdownEvent(10);     // 计数器，初始值 10

for (int i = 0; i < 10; i++)
{
    ThreadPool.QueueUserWorkItem(state =>
    {
        Calc(state);
        done.Signal();                       // 完成一个，计数减 1
    }, i);
}

done.Wait();                                 // 阻塞，直到计数减到 0
Console.WriteLine("all done（输出顺序每次可能不同）");

static void Calc(object? param)              // WaitCallback 的参数是 object?
{
    if (param is int number)
        Console.WriteLine($"{number} * {number} = {number * number}");
}
```
```
1 * 1 = 1
2 * 2 = 4
7 * 7 = 49
3 * 3 = 9
4 * 4 = 16
6 * 6 = 36
0 * 0 = 0
5 * 5 = 25
8 * 8 = 64
9 * 9 = 81
all done（输出顺序每次可能不同）
```

缺点：拿不到返回值、不能直接等待、不能取消、异常会让进程崩溃。所以现在一般用 `Task.Run`（第 12 章），它也是放进同一个线程池，但返回一个 `Task`，能 `await`、拿结果、传异常。

| Java | C# |
|---|---|
| `executor.execute(runnable)` | `ThreadPool.QueueUserWorkItem(callback)` |
| `executor.submit(callable)` 返回 `Future` | `Task.Run(func)` 返回 `Task<T>` |
| `executor.shutdown()` | 不需要，全局线程池随进程退出 |

## 4. 用信号等待其他线程 🟡

线程之间"我做完了，你可以继续了"的通知，叫**信号**。

| 类型 | 比喻 | 用法 | Java 对应 |
|---|---|---|---|
| `ManualResetEvent(Slim)` | 一盏灯：`Set` 点亮后一直亮，所有等待者都放行，直到 `Reset` | 一次性的"准备好了"通知 | — |
| `AutoResetEvent` | 闸机：`Set` 一次只放行一个等待者，然后自动关上 | 一次放一个 | — |
| `CountdownEvent` | 倒计时：`Signal` 一次减 1，减到 0 时放行 | 等 N 个任务都完成 | `CountDownLatch` |
| `Barrier` | 集合点：N 个线程都到了才一起继续 | 分阶段并行计算 | `CyclicBarrier` |

### 4.1 ManualResetEvent + WaitAll

每个任务一盏灯，全亮了主线程再继续（[04-manual-reset-event.cs](../advanced/04-manual-reset-event.cs)）：

```csharp
internal class Model
{
    public ManualResetEvent Wait { get; }
    public int Number { get; private set; }
    public int Result { get; private set; }
    public int Milliseconds { get; private set; }
    public Model(ManualResetEvent wait) => Wait = wait;

    public void Calc(object? param)
    {
        if (param is int number) { Number = number; Result = number * number; }
        Milliseconds = Random.Shared.Next(0, 500);
        Thread.Sleep(Milliseconds);
        Wait.Set();                           // 点亮：我完成了
    }
}

List<Model> models = [];
List<ManualResetEvent> waits = [];
for (int i = 0; i < 10; i++)
{
    var model = new Model(new ManualResetEvent(false));   // false = 一开始是灭的
    models.Add(model);
    waits.Add(model.Wait);
    ThreadPool.QueueUserWorkItem(model.Calc, i);
}

WaitHandle.WaitAll(waits.ToArray());          // 等所有灯都亮（最多 64 个）

foreach (var item in models)
{
    Console.WriteLine($"{item.Number} * {item.Number} = {item.Result}, sleep {item.Milliseconds}ms");
    item.Wait.Dispose();                      // 包装了系统句柄，用完释放
}

// 现代写法：Task.WhenAll，不需要信号灯，也没有 64 个的限制
var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
{
    await Task.Delay(Random.Shared.Next(0, 500));
    return i * i;
}));
int[] results = await Task.WhenAll(tasks);
Console.WriteLine($"Task.WhenAll: {string.Join(", ", results)}");
```
```
0 * 0 = 0, sleep 140ms
1 * 1 = 1, sleep 335ms
2 * 2 = 4, sleep 439ms
...
9 * 9 = 81, sleep 365ms
Task.WhenAll: 0, 1, 4, 9, 16, 25, 36, 49, 64, 81
```

完成顺序是随机的，但输出是按 `models` 的创建顺序遍历的，所以结果总是有序的（sleep 毫秒数每次不同）。

- ⚠️ `WaitHandle.WaitAll` 最多等 **64 个**句柄，超过会抛异常。
- `ManualResetEvent` 包装的是操作系统内核对象，较重；只在进程内用时，优先用 **`ManualResetEventSlim`**（先自旋一会儿再阻塞，等待时间短时更快）。
- 用完要 `Dispose`，或者用 `using var`。

### 4.2 AutoResetEvent：一次只放一个

```csharp
using var gate = new AutoResetEvent(false);
for (int i = 1; i <= 3; i++)
{
    int id = i;                                   // 先复制一份，避免捕获循环变量
    new Thread(() => { gate.WaitOne(); Console.WriteLine($"线程 {id} 通过"); }).Start();
}
Thread.Sleep(200);
Console.WriteLine("Set 一次");
gate.Set();
Thread.Sleep(200);
Console.WriteLine("再 Set 两次");
gate.Set(); Thread.Sleep(100); gate.Set();
```
```
Set 一次
线程 1 通过
再 Set 两次
线程 2 通过
线程 3 通过
```

`Set` 一次只放行一个线程（放行哪一个不保证）。换成 `ManualResetEvent`，第一次 `Set` 三个线程就全部通过了。

### 4.3 CountdownEvent

上面 3.4 节已经用过：`new CountdownEvent(N)`，每个任务完成调 `Signal()`，主线程 `Wait()` 等计数到 0。没有 64 个的限制，比一堆 `ManualResetEvent` 简单。等价于 Java 的 `CountDownLatch`（`countDown()` / `await()`）。

⚠️ 比 `Thread.Sleep(1000)` "估计一下等多久"可靠得多：睡短了任务没做完，睡长了白白浪费时间。

## 5. ExecutionContext 与 AsyncLocal 🟡

### 5.1 ThreadLocal 在 Task 里不好用

Java 里常用 `ThreadLocal` 保存"当前用户""当前请求 id"。C# 也有 `ThreadLocal<T>` 和 `[ThreadStatic]`，但在 async 代码里，一个请求会在多个线程之间跳来跳去，**按线程保存的值就丢了**：

```csharp
static readonly ThreadLocal<string?> tl = new();
static readonly AsyncLocal<string?> al = new();
[ThreadStatic] static string? ts;

static async Task Main(string[] args)
{
    tl.Value = "Owen"; al.Value = "Owen"; ts = "Owen";
    await Task.Run(() =>
        Console.WriteLine($"Task.Run 里: ThreadLocal={tl.Value ?? "null"}, ThreadStatic={ts ?? "null"}, AsyncLocal={al.Value ?? "null"}"));
}
```
```
Task.Run 里: ThreadLocal=null, ThreadStatic=null, AsyncLocal=Owen
```

| | 值属于谁 | 换线程后 | 用在哪 |
|---|---|---|---|
| `ThreadLocal<T>` / `[ThreadStatic]` | 某个线程 | 读不到 | 每个线程一份的缓存（比如非线程安全对象） |
| `AsyncLocal<T>` | 某条**执行流** | 跟着 `Task.Run`、`await`、线程池流过去 | 当前用户、请求 id、日志上下文 |

### 5.2 ExecutionContext 流动

`AsyncLocal` 的值保存在**执行上下文（ExecutionContext）**里。你把工作交给别的线程（`Task.Run`、`QueueUserWorkItem`、`await` 之后继续执行）时，.NET 会把当前的执行上下文**复制**过去，所以能读到。

老教程里的 `CallContext.LogicalSetData` / `LogicalGetData` 是 .NET Framework 的 API（属于 Remoting），**现代 .NET 里已经没有这个类**，用 `AsyncLocal<T>` 代替。

`ExecutionContext.SuppressFlow()` 可以暂停流动（[04-execution-context-asynclocal.cs](../advanced/04-execution-context-asynclocal.cs)）：

```csharp
internal static class Context
{
    public static readonly AsyncLocal<Person?> CurrentPerson = new();
}

var person = new Person { Name = "Owen", Age = 36 };
Context.CurrentPerson.Value = person;        // 相当于 CallContext.LogicalSetData("person", person)

using var done = new CountdownEvent(3);

ThreadPool.QueueUserWorkItem(_ => { Show("默认"); done.Signal(); });         // 上下文流过去 → 能读到

ExecutionContext.SuppressFlow();                                              // 暂停流动 → 读不到
ThreadPool.QueueUserWorkItem(_ => { Show("SuppressFlow"); done.Signal(); });
ExecutionContext.RestoreFlow();

ThreadPool.QueueUserWorkItem(_ => { Show("RestoreFlow"); done.Signal(); });   // 恢复 → 又能读到

done.Wait();

static void Show(string label)
{
    Person? p = Context.CurrentPerson.Value;
    Console.WriteLine($"[{label}] 线程 {Environment.CurrentManagedThreadId}: " +
                      (p is null ? "person is empty" : $"person: {p.Name}, {p.Age}"));
}
```
```
[SuppressFlow] 线程 4: person is empty
[默认] 线程 6: person: Owen, 36
[RestoreFlow] 线程 7: person: Owen, 36
```

（输出顺序每次可能不同。）

- `SuppressFlow` 很少用，主要是框架内部用来避免把上下文泄漏到长期存在的后台任务里。用完一定要 `RestoreFlow`。
- 流动是**复制**：子任务里给 `AsyncLocal` 重新赋值，不会影响父流程里的值。但如果值是引用类型，改的是同一个对象的属性，父流程能看到。
- Java 对比：`InheritableThreadLocal` 只在"创建子线程"时复制一次，线程池里复用线程就失效了；`AsyncLocal` 每次交给线程池都会流动。

## 6. 线程里的未处理异常会让进程崩溃 ⚠️

```csharp
var t = new Thread(() => throw new InvalidOperationException("子线程出错"));
try
{
    t.Start();
    t.Join();
}
catch (Exception e)
{
    Console.WriteLine($"Main 捕获到了: {e.Message}");   // 不会执行
}
Console.WriteLine("Main 结束");                          // 也不会执行
```
```
Unhandled exception. System.InvalidOperationException: 子线程出错
   at HelloWorld.Program.<>c.<Main>b__0_0() in .../crash.cs:line 7
   at System.Threading.Thread.StartCallback(Thread* pThread)
```

进程退出码 134（被中止）。

- 主线程的 `try/catch` **抓不到**别的线程抛的异常：异常只沿着抛出它的那个线程的调用栈往上找 `catch`。
- 和 Java 不同：Java 子线程未捕获的异常只会打印堆栈、结束那个线程，进程照常运行；**C# 里任何线程（包括线程池线程、`QueueUserWorkItem`）的未处理异常都会让整个进程崩溃**。
- 所以 `new Thread` / `QueueUserWorkItem` 的方法体里要自己 `try/catch`。`Task` 不会这样，它把异常保存起来，`await` 时再抛出。详细见第 13 章。

## 7. 怎么选 & 与 Java 对照

| 场景 | 选择 |
|---|---|
| 等待 I/O（文件、网络、数据库） | `async/await`，**不需要线程**（第 13 章） |
| CPU 密集的计算，不想阻塞当前线程 | `Task.Run`（第 12 章） |
| 长时间运行的后台循环 | `new Thread`（设 `IsBackground`）或 `Task.Factory.StartNew(..., LongRunning)` |
| 需要设置线程名、优先级、STA | `new Thread` |
| 等 N 个任务完成 | `await Task.WhenAll`；老代码里用 `CountdownEvent` |
| 保存"当前请求"上下文 | `AsyncLocal<T>` |

| Java | C# |
|---|---|
| `Thread` / `Runnable` | `Thread` / `ThreadStart`（委托） |
| `setDaemon(true)` | `IsBackground = true` |
| `ExecutorService` / `Executors.newFixedThreadPool` | 全局 `ThreadPool`，一般通过 `Task.Run` 使用 |
| `executor.submit(callable)` → `Future<T>` | `Task.Run(func)` → `Task<T>` |
| `CountDownLatch` | `CountdownEvent` |
| `CyclicBarrier` | `Barrier` |
| `ThreadLocal` | `ThreadLocal<T>` / `[ThreadStatic]` |
| `InheritableThreadLocal` | `AsyncLocal<T>`（更强：跟着 await 和线程池流动） |
| 子线程异常只结束该线程 | 子线程未处理异常让**整个进程崩溃** |

## 8. 小结

1. `new Thread(方法).Start()` 新建专用线程，`Join()` 等它结束；传参优先用 lambda 捕获，循环变量先复制一份。
2. 前台线程会阻止进程退出；后台线程随进程退出被直接杀掉。`new Thread` 默认前台，线程池线程全是后台。
3. 线程池是全局的：最小工作线程数默认 = CPU 核数，超过后大约每 0.5 秒才加一个线程。
4. 在线程池线程里阻塞（`Sleep`、`.Result`、同步 I/O）会导致线程池饥饿，实测 30 个阻塞 2 秒的任务用了 6 秒；用 `async/await` 代替阻塞。
5. `QueueUserWorkItem` 拿不到结果也不能等待，要配合 `CountdownEvent` / `ManualResetEvent`；新代码用 `Task.Run`。
6. 信号：`ManualResetEvent(Slim)` 一亮全放行，`AutoResetEvent` 一次放一个，`CountdownEvent` 等 N 个；`WaitHandle.WaitAll` 最多 64 个。
7. `ThreadLocal` 换线程就丢；`AsyncLocal` 存在 ExecutionContext 里，会跟着 Task、await、线程池流动；`CallContext` 在现代 .NET 里已经没有了。
8. 任何线程的未处理异常都会让进程崩溃，主线程的 `catch` 抓不到；`Thread` 的方法体里要自己 `try/catch`。

---

上一章：[反射与特性 🟡](10-reflection-attributes.md) · [目录](../README.md) · 下一章：[Task、并行与定时器 🟢](12-task-parallel.md)
