# C# 多线程与 Task：创建线程的方式与等待任务

严格地说，**真正"创建新线程"的只有两种**：`new Thread` 和 `TaskCreationOptions.LongRunning`。
其他方式都是**把代码交给线程池里现有的线程执行**。

文中标注"实测"的结论都在 .NET 10 上运行验证过，验证代码见 `workspace6/interview-docs/verify/csharp-thread/Program.cs`：

```bash
dotnet run          # 打印每种方式所在线程的信息：线程 id、是否线程池线程、是否后台线程
dotnet run -- fg    # 前台线程：主程序返回后，进程会等这个线程执行完
dotnet run -- bg    # 后台线程：主程序返回后，这个线程直接被终止
```

## 总览

| 方式 | 线程池线程 | 后台线程 | 现状 | 对应文件（advanced/） |
|---|---|---|---|---|
| `new Thread(...)` | 否，每次**新建**专用线程 | 默认不是（前台线程） | 只在特殊场景用 | 04-thread-start、04-thread-parameterized |
| `Task.Factory.StartNew(..., LongRunning)` | 否，单独**新建**线程 | 是 | 长时间运行的任务才用 | — |
| `ThreadPool.QueueUserWorkItem` | 是 | 是 | 老写法，一般改用 Task | 04-threadpool-queue-work-item、04-manual-reset-event |
| `new Task(...)` + `Start()` | 是 | 是 | 少用 | 05-task-new-start |
| `Task.Factory.StartNew` | 是 | 是 | 需要特殊选项时用 | 05-task-factory-startnew |
| **`Task.Run`** | 是 | 是 | ✅ **首选** | 05-task-run |
| `ContinueWith` / `await` 之后的代码 | 是 | 是 | ✅ await 常用 | 05-task-continuation、08-async-await-basics |
| `Parallel.For` / `ForEach` / `Invoke`、PLINQ | 是，自动分配到多个线程 | 是 | ✅ CPU 密集的数据并行 | 07-parallel-* |
| 定时器回调（`System.Threading.Timer`、`System.Timers.Timer`、`PeriodicTimer`） | 是 | 是 | ✅ 定时任务 | 10-threading-timer、10-timers-timer、10-periodic-timer |
| `BackgroundWorker` | 是 | 是 | 只在老 WinForms 项目里见到 | — |
| 委托 `BeginInvoke` | — | — | ❌ .NET Core 起**不再支持**，实测抛 `PlatformNotSupportedException` | 05-task-continuation（替代写法） |

## `new Thread` 和 `Task.Run` 的区别

| | `new Thread(...).Start()` | `Task.Run(...)` |
|---|---|---|
| 线程从哪来 | **新建**一个专用线程 | **借用**线程池里的现成线程 |
| 用完之后 | 线程销毁 | 线程还回线程池，接着执行别的任务 |
| 前台 / 后台 | 默认是前台线程 | 线程池线程都是后台线程 |
| 创建成本 | 高（每次都要向操作系统申请，默认预留约 1MB 栈空间） | 低（复用） |
| 返回值 | 拿不到 | `Task<T>` |
| 未捕获的异常 | 直接让进程崩溃 | 保存在 Task 里，`await` 时抛出 |

## 1. Thread 类

```csharp
var t = new Thread(() => Console.WriteLine("work"));
t.IsBackground = true;   // 可选，默认是前台线程
t.Start();
t.Join();                // 等待线程结束

new Thread(o => Console.WriteLine(o)).Start(42);  // 传参，参数类型是 object?
```

- 可以完全控制线程：名称、优先级、前台/后台、单元模式（`SetApartmentState(STA)`，有些 COM 或 UI 组件需要）。
- 缺点：
  - 创建和销毁线程开销大，每个线程默认要预留约 1MB 的栈空间。
  - 拿不到返回值。
  - 线程里没有捕获的异常会直接让进程崩溃。
- **前台线程和后台线程**：只要还有前台线程在运行，进程就不会退出；后台线程会在主线程结束时被强行终止。
  - 实测：主程序直接返回后，前台线程仍然打印出了"跑完了"，后台线程没有打印。

## 2. ThreadPool

```csharp
ThreadPool.QueueUserWorkItem(_ => Console.WriteLine("work"));
```

- 线程池里的线程会被复用，开销小。
- 缺点：拿不到返回值，不能等待它完成，也不能取消，要自己用 `CountdownEvent`、`ManualResetEvent` 等同步。
- 线程池一开始只有 CPU 核数个线程，超过之后大约每 0.5 秒才增加一个。在线程池线程里阻塞（`Thread.Sleep`、`.Result`、同步 I/O）会导致"线程池饥饿"。

## 3. Task（推荐）

```csharp
// Task 三兄弟（底层都是线程池）
var t1 = new Task(() => Console.WriteLine("new Task"));
t1.Start();                                               // 冷启动，必须调用 Start
var t2 = Task.Factory.StartNew(() => Console.WriteLine("StartNew"));
var t3 = Task.Run(() => Console.WriteLine("Task.Run"));  // 推荐

int r = await Task.Run(() => 1 + 1);                      // 有返回值，实测 r = 2

try { await Task.Run(() => throw new InvalidOperationException("boom")); }
catch (InvalidOperationException e) { }                   // 异常在 await 时抛出，可以捕获

// LongRunning：单独新建线程，避免长时间运行的任务占住线程池（实测 pool=False）
await Task.Factory.StartNew(Work, TaskCreationOptions.LongRunning);

// 延续：前一个任务完成后，在线程池上执行
var t5 = Task.Run(() => 42).ContinueWith(t => Console.WriteLine($"ContinueWith {t.Result}"));
```

- 默认运行在线程池上。
- 支持返回值、`await`、异常传递、取消（`CancellationToken`）和组合（`WhenAll` / `WhenAny`）。
- `Task.Run` 相当于参数取了合适默认值的 `Task.Factory.StartNew`。只有需要 `LongRunning` 之类的选项时才用 `StartNew`。

验证 `Task.Run` 用的是线程池线程：

```csharp
Console.WriteLine($"Main: 线程 {Environment.CurrentManagedThreadId}, 线程池线程 = {Thread.CurrentThread.IsThreadPoolThread}");

await Task.Run(() =>
    Console.WriteLine($"Task.Run: 线程 {Environment.CurrentManagedThreadId}, 线程池线程 = {Thread.CurrentThread.IsThreadPoolThread}"));
// Task.Run 里打印的 IsThreadPoolThread 是 True；多次执行会看到线程 ID 被复用
```

## 4. Parallel / PLINQ

```csharp
Parallel.For(0, 100, i => Compute(i));
Parallel.ForEach(items, item => Handle(item));
Parallel.Invoke(Method1, Method2, Method3);
var results = data.AsParallel().Select(Compute).ToList();
```

- 把任务自动分配到多个线程池线程上执行（实测用到了多个线程），适合 CPU 密集的计算。
- 调用会**阻塞**到全部执行完毕；调用它的线程自己也会参与执行。不想阻塞调用方时，外面包一层 `Task.Run`。

## 5. 定时器与 BackgroundWorker

```csharp
// 回调在线程池线程上执行
var timer = new System.Threading.Timer(_ => Tick(), null, dueTime: 0, period: 1000);

// 新写法，配合 await 使用
using var periodic = new PeriodicTimer(TimeSpan.FromSeconds(1));
while (await periodic.WaitForNextTickAsync()) { /* ... */ }

var bw = new BackgroundWorker();
bw.DoWork += (s, e) => DoWork();                   // 在线程池上执行
bw.RunWorkerCompleted += (s, e) => UpdateUI();     // 在 UI 线程上执行
bw.RunWorkerAsync();
```

- `BackgroundWorker` 是 WinForms 时代的写法，现在一般用 `async` / `await` 代替。

## async/await：等待期间不占线程

`await` 一个 I/O 操作（读文件、网络请求、数据库查询）时，**等待期间不占用任何线程**，完成后再从线程池里取一个线程继续执行。它不属于"开线程"，但现代 C# 中大部分"并发"都是靠它实现的。

```csharp
await Task.Run(async () =>
{
    Console.WriteLine(Environment.CurrentManagedThreadId);   // 线程 A
    await Task.Delay(1000);                                  // 等待期间，线程 A 还回线程池
    Console.WriteLine(Environment.CurrentManagedThreadId);   // 可能是线程 B
});
```

## 怎么选

| 场景 | 选择 |
|---|---|
| 等待 I/O（文件、网络、数据库） | `async` / `await`，**不需要开线程** |
| CPU 密集的计算，不想阻塞当前线程 | `Task.Run` |
| 一批数据需要并行计算 | `Parallel.For` / `ForEach`、PLINQ |
| 长时间运行的后台循环 | `Task.Factory.StartNew(..., LongRunning)`，或者 `new Thread` |
| 定时执行 | `PeriodicTimer`（async 代码），或者 `System.Threading.Timer` |
| 需要控制线程优先级、前台/后台、STA 等底层属性 | `new Thread` |

## 常见追问

- **`async` / `await` 会创建线程吗？** 不会。
  - `async` 方法在第一个 `await` 之前，和调用者在同一个线程上同步执行（实测确认）。
  - `await` 一个 I/O 操作时，等待期间不占用任何线程。
  - 真正把工作放到其他线程上执行的是 `Task.Run`。
- **I/O 密集和 CPU 密集分别怎么做？**
  - I/O 密集：直接 `await` 异步 API，比如 `HttpClient` 和文件读写。
  - CPU 密集：用 `Task.Run` 或 `Parallel`。
  - 不要用 `Task.Run` 包装一个异步的 I/O 方法。
- **为什么不推荐直接 `new Thread`？** 开销大，拿不到返回值，异常会让进程崩溃，也难以取消和组合。只有需要长期运行的专用线程、STA 线程，或者要设置线程优先级时才用它。
- **`Task.Run` 到底会不会创建线程？** `Task.Run` 本身不会，它只是把任务放进线程池的队列；但线程池可能会根据负载创建线程。
  - 程序刚启动时线程池是空的（`ThreadCount = 0`），第一次 `Task.Run` 时才按需创建。
  - 有空闲线程就复用：顺序执行 100 次 `Task.Run`，实测只用到 2 个线程。
  - 线程都被阻塞时才逐步新建：30 个 `Thread.Sleep(2000)` 的任务，3 秒后线程数从 3 涨到 12（CPU 8 核）。线程数达到最小值（默认等于 CPU 核数）之后，大约每 0.5 秒才加一个。
  - 对比 `new Thread`：每次调用都一定新建一个专用线程，用完就销毁。
  - 打个比方：`Task.Run` 是"叫号排队"，线程池是"柜台"。排队不会增加柜台；只有当所有柜台都忙不过来时，才会慢慢加开柜台。

## 创建 Task 的方式

`Task` 只代表"一个将来会完成的操作"，不一定对应一个线程。得到 Task 的方式可以分成五类：

| 类别 | 方式 | 是否在线程池上执行代码 | 典型用途 |
|---|---|---|---|
| **启动一段代码** | `new Task(...)` + `Start()` | 是（冷启动，必须调用 Start） | 先创建、稍后再决定何时启动 |
| | `Task.Factory.StartNew(...)` | 是（`LongRunning` 时新建线程） | 需要特殊选项时 |
| | **`Task.Run(...)`** | 是 | ✅ CPU 密集的工作，最常用 |
| **编译器生成** | **`async` 方法** | 第一个 await 前在调用方线程同步执行 | ✅ 日常写异步代码 |
| **已完成的 Task** | `Task.FromResult(x)`、`Task.CompletedTask` | 否，创建时就已完成 | 方法签名要求返回 Task，但结果已经有了（缓存、测试） |
| | `Task.FromException(ex)`、`Task.FromCanceled(token)` | 否，创建时就已失败 / 已取消 | 同上，表示失败或取消 |
| **手动控制完成时机** | `TaskCompletionSource<T>` | 否，由你调用 `SetResult` 决定何时完成 | 把回调、事件包装成可以 await 的 Task |
| **由其他 Task 组合而来** | `ContinueWith`、`Task.WhenAll`、`Task.WhenAny`、`Task.Delay`、`WaitAsync` | 视情况 | 延续、等待多个、延时、超时 |
| | `Task.Factory.FromAsync(BeginXxx, EndXxx, ...)` | 否 | 把老的 APM 写法（BeginRead/EndRead）包装成 Task |

```csharp
// 1. 启动一段代码
var t1 = new Task<int>(() => 1); t1.Start();
var t2 = Task.Factory.StartNew(() => 2);
var t3 = Task.Run(() => 3);

// 2. async 方法：编译器自动生成并返回 Task
Task<int> t4 = GetAsync();
static async Task<int> GetAsync() { await Task.Delay(10); return 4; }

// 3. 已完成的 Task
Task<int> t5 = Task.FromResult(5);
Task t6 = Task.CompletedTask;
Task<int> t7 = Task.FromException<int>(new InvalidOperationException("x"));   // Status = Faulted
Task<int> t8 = Task.FromCanceled<int>(new CancellationToken(true));            // Status = Canceled

// 4. TaskCompletionSource：手动决定什么时候完成（这里用定时器在 100ms 后完成它）
var tcs = new TaskCompletionSource<int>();
var timer = new Timer(_ => tcs.TrySetResult(9), null, 100, Timeout.Infinite);
Task<int> t9 = tcs.Task;

// 5. 由其他 Task 组合而来
Task<int> t10 = t3.ContinueWith(t => t.Result * 10);
Task<int[]> t11 = Task.WhenAll(t1, t2, t3);
Task<Task<int>> t12 = Task.WhenAny(t1, t2);
Task t13 = Task.Delay(100);
Task<int> t14 = Task.Factory.FromAsync(fs.BeginRead, fs.EndRead, buffer, 0, buffer.Length, null);
```

实测输出（依次 await 上面的 Task，或者查看它们的 Status）：

```
1 2 3 4 5 RanToCompletion Faulted Canceled 9 30 [1,2,3] RanToCompletion 16
```

要点：

- **只有第 1 类会把你的代码放到线程池上执行**。其他几类本身不占用线程。
- `async` 方法是最常见的 Task 来源，编译器背后用的就是类似 `TaskCompletionSource` 的机制：方法里的 `return x` 被改写成"设置 Task 的结果"。
- `TaskCompletionSource` 是把"非 Task 的异步"（事件、回调、老 API）接入 async/await 的通用工具：
  ```csharp
  Task<string> WaitForMessageAsync(Button button)
  {
      var tcs = new TaskCompletionSource<string>();
      button.Click += (s, e) => tcs.TrySetResult("clicked");   // 事件触发时完成 Task
      return tcs.Task;                                         // 调用方可以 await 它
  }
  ```
- 在同步方法里需要返回 Task 时，用 `Task.FromResult` / `Task.CompletedTask`，不要写 `Task.Run(() => x)`，否则会白白占用一个线程池线程。

## 等待多个任务：WaitAll vs WhenAll

- `Task.WaitAll(...)`：**阻塞**当前线程，原地等，直到全部完成才返回。
- `Task.WhenAll(...)`：**不阻塞**，马上返回一个新的 Task，代表"全部完成"这件事，再用 `await` 去等它。

```csharp
Task.WaitAll(t1, t2);                       // 当前线程卡在这里，什么也做不了
await Task.WhenAll(t1, t2);                 // 等待期间，当前线程被释放，可以去处理别的工作
int[] results = await Task.WhenAll(t3, t4); // 直接拿到结果数组，顺序和传入的顺序一致
```

| | `Task.WaitAll` | `Task.WhenAll` |
|---|---|---|
| 等待方式 | **同步阻塞** | **异步**，返回 Task，配合 `await` |
| 返回值 | `void`（带超时的重载返回 `bool`） | `Task`，或者 `Task<TResult[]>` |
| 拿结果 | 还要再逐个读 `t1.Result`、`t2.Result` | `await` 直接得到结果数组 |
| 异常 | 抛 `AggregateException`，里面包含**所有**异常 | `await` 时抛出**第一个**异常本身；所有异常都保存在返回的 Task 的 `.Exception` 里 |
| 超时 / 取消 | 有重载：`WaitAll(tasks, 5000)`、`WaitAll(tasks, token)` | 没有这类参数，可以写 `await Task.WhenAll(...).WaitAsync(TimeSpan.FromSeconds(5))` |
| 用在哪里 | 普通的同步方法，比如 `void Main` | `async` 方法里 |

异常的区别：

```csharp
Task a = Task.Run(() => throw new InvalidOperationException("A"));
Task b = Task.Run(() => throw new ArgumentException("B"));

try { Task.WaitAll(a, b); }
catch (AggregateException ex)
{
    // ex.InnerExceptions 里有 A 和 B 两个异常
}

Task all = Task.WhenAll(a, b);
try { await all; }
catch (Exception ex)
{
    // ex 是第一个异常（A），类型就是 InvalidOperationException，不是 AggregateException
    // 想看全部异常：all.Exception!.InnerExceptions
}
```

为什么在 async 代码里要用 `WhenAll`：

1. **阻塞线程很浪费**：`WaitAll` 让一个线程什么都不做地干等，在线程池线程上阻塞还会导致线程池饥饿。
2. **可能死锁**：在 WinForms、WPF 这类有 UI 线程的程序里，在 UI 线程上调用 `WaitAll`（以及 `.Wait()`、`.Result`），而被等待的任务里有 `await` 需要回到 UI 线程继续执行。UI 线程在等任务，任务在等 UI 线程，就死锁了。

**原则**：在 `async` 方法里一律用 `await Task.WhenAll`；只有在不能使用 async 的同步代码里，才用 `Task.WaitAll`。

同理，`WaitAny` 和 `WhenAny`：

```csharp
int index = Task.WaitAny(t1, t2);                  // 阻塞，返回最先完成的任务的下标
Task first = await Task.WhenAny(t1, t2);           // 异步，返回最先完成的那个 Task

// 常用来实现"谁先完成用谁"，或者自己实现超时
var work = DownloadAsync();
if (await Task.WhenAny(work, Task.Delay(3000)) == work)
    Console.WriteLine("下载完成");
else
    Console.WriteLine("超时了");
```

## 要不要用 Task.Run 包装异步方法

准确的说法是：**一个真正异步的方法（比如 I/O 操作），直接 `await` 它就行，不需要再用 `Task.Run` 包一层。**

### 为什么不需要包

```csharp
// ❌ 多此一举
await Task.Run(async () => await httpClient.GetStringAsync(url));

// ✅ 直接 await
await httpClient.GetStringAsync(url);
```

`GetStringAsync` 本身就是异步的，发出请求之后，等待期间**不占用任何线程**。用 `Task.Run` 包一层的结果是：

1. 先从线程池借一个线程，
2. 这个线程只做了"发出请求"这么一件小事，遇到 await 就还回线程池，
3. 等待的过程和不包装时完全一样。

白白多了一次线程切换和调度，没有任何好处。

**也不需要用它来"让多个异步方法同时执行"**：调用异步方法时，它就已经开始执行了。

```csharp
// ✅ 两个请求已经同时在进行了，不需要 Task.Run
Task<string> a = httpClient.GetStringAsync(url1);
Task<string> b = httpClient.GetStringAsync(url2);
await Task.WhenAll(a, b);
```

### 这些情况下，包一层是有意义的

**1. 异步方法里有耗时的同步代码**

async 方法在第一个 await 之前，是在调用方线程上**同步执行**的：

```csharp
async Task ProcessAsync()
{
    var data = HeavyCompute();          // ← 耗时 2 秒的 CPU 计算，在调用方线程上同步执行
    await File.WriteAllTextAsync(path, data);
}
```

如果调用方是 UI 线程，界面会卡住 2 秒。这时 `await Task.Run(() => ProcessAsync())` 把它整体放到线程池上执行是合理的。更好的做法是修改方法本身，只把 CPU 计算那部分放进 `Task.Run`：

```csharp
var data = await Task.Run(() => HeavyCompute());
```

**2. "假异步"方法**

有些方法签名返回 Task，内部却是同步阻塞的（比如老库里 `return Task.FromResult(SyncCall())` 这种写法）。在 UI 线程上调用会卡住，只能用 `Task.Run` 把它挪到别的线程上。

**3. 在同步代码里调用异步方法（不得已时）**

```csharp
var result = Task.Run(() => FooAsync()).Result;   // 迫不得已的权宜之计
```

在有 UI 线程的程序里直接写 `FooAsync().Result` 可能会死锁。包一层 `Task.Run`，让 FooAsync 在没有同步上下文的线程池上执行，就能避免死锁。但这只是权宜之计，最好的办法还是把调用链改成 async。

### 反过来也一样：不要用 Task.Run 把同步方法包装成"异步方法"

```csharp
// ❌ 在类库里这样写
public Task<int> CalculateAsync() => Task.Run(() => Calculate());
```

这叫"async over sync"：方法名看起来是异步的，实际上只是换了个线程阻塞，调用方还以为它不占线程。正确的做法是**类库只提供同步的 `Calculate()`**，由调用方根据自己的情况决定要不要用 `Task.Run`：UI 程序需要，服务器程序通常不需要。

### 总结

| 情况 | 要不要用 Task.Run |
|---|---|
| 真正异步的 I/O 方法（HttpClient、文件、数据库） | ❌ 直接 await |
| 想让多个异步方法同时执行 | ❌ 直接调用，再用 WhenAll 等待 |
| CPU 密集的同步计算，不想阻塞 UI 线程 | ✅ |
| 异步方法里有很重的同步部分，或者是"假异步" | ✅（在 UI 程序里） |
| 在类库里把同步方法包装成 XxxAsync | ❌ 交给调用方决定 |
| ASP.NET Core 这类服务器程序 | 基本不需要，只会多一次线程切换 |

一句话：**`Task.Run` 是用来把 CPU 工作挪到别的线程上的，不是用来"让代码变成异步"的。**

## 实测输出（.NET 10）

```
Main                         id=1   pool=False bg=False
Thread(lambda)               id=4   pool=False bg=False
Thread(param=42)             id=5   pool=False bg=False
Thread IsBackground=true     id=6   pool=False bg=True
ThreadPool.QueueUserWorkItem id=7   pool=True  bg=True
Task.Run                     id=9   pool=True  bg=True
StartNew LongRunning         id=11  pool=False bg=True
Task.Run 返回值 = 2
await 捕获 Task 异常: boom
Parallel.For 用到的线程数 > 1: True，包含主线程: False
Threading.Timer 回调           id=16  pool=True  bg=True
BackgroundWorker.DoWork      id=16  pool=True  bg=True
BeginInvoke: PlatformNotSupportedException
async 方法 await 前与调用者同线程: True
```

- 线程 id 每次运行可能不同。
- 前面的代码 `await` 过 Task，之后的代码已经不在 1 号线程上运行了，所以 `Parallel.For` 一行显示"包含主线程: False"。

## 和 Java 的对应关系

| C# | Java |
|---|---|
| `new Thread` | `new Thread` |
| `ThreadPool` / `Task.Run` | `ExecutorService.submit` |
| `Task<T>` / `ContinueWith` | `CompletableFuture` / `thenApply` |
| `Parallel.For` / PLINQ | `parallelStream()` |
