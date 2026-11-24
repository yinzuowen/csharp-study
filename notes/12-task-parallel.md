# 第 12 章 Task、并行与定时器 🟢

> 本章练习：[05-task-run.cs](../advanced/05-task-run.cs)、[05-task-new-start.cs](../advanced/05-task-new-start.cs)、[05-task-factory-startnew.cs](../advanced/05-task-factory-startnew.cs)、[05-task-wait.cs](../advanced/05-task-wait.cs)、[05-task-wait-cancellation.cs](../advanced/05-task-wait-cancellation.cs)、[05-task-cancellation.cs](../advanced/05-task-cancellation.cs)、[05-task-cancel-partial-result.cs](../advanced/05-task-cancel-partial-result.cs)、[05-task-continuation.cs](../advanced/05-task-continuation.cs)、[05-task-many-tasks.cs](../advanced/05-task-many-tasks.cs)、[07-parallel-invoke.cs](../advanced/07-parallel-invoke.cs)、[07-parallel-in-task-run.cs](../advanced/07-parallel-in-task-run.cs)、[07-parallel-foreach.cs](../advanced/07-parallel-foreach.cs)、[07-parallel-for-break-stop.cs](../advanced/07-parallel-for-break-stop.cs)、[07-parallel-for-thread-local.cs](../advanced/07-parallel-for-thread-local.cs)、[10-threading-timer.cs](../advanced/10-threading-timer.cs)、[10-timers-timer.cs](../advanced/10-timers-timer.cs)、[10-periodic-timer.cs](../advanced/10-periodic-timer.cs)

> 写代码时你会遇到这些问题：想在后台算点东西，该 `new Thread` 还是 `Task.Run`？`Task.Run(DoJob, 456)` 为什么编译不过，报错还看不懂？`task.Wait(2000)` 超时了，任务怎么还在跑？调了 `cts.Cancel()`，任务却没停？`Parallel.For` 里累加结果对不上？定时器写好了，程序一运行就退出，回调一次都没执行？
> 本章讲 Java 里 `ExecutorService` + `Future` + `CompletableFuture` + `ForkJoin` + `ScheduledExecutorService` 在 C# 里的对应物：Task、取消、延续、并行循环和三种定时器。`Thread` / `ThreadPool` 本身见[第 11 章](11-threads.md)，`async` / `await` 见[第 13 章](13-async-await.md)。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果（本机 CPU 8 核）。多线程的输出顺序、线程 id 每次运行可能不同。

## 1. Task 是什么 🟢

`Task` 代表"一个将来会完成的操作"，相当于 Java 的 `Future` + `CompletableFuture` 合体：

- 能拿返回值：`Task<T>`
- 能等待：`Wait()`、`.Result`、`await`
- 能取消：`CancellationToken`
- 能组合：`ContinueWith`、`WhenAll`、`WhenAny`
- 异常不会让进程崩溃，而是保存在 Task 里，等你去取（详见第 13 章）

| Java | C# |
|---|---|
| `executor.submit(callable)` 返回 `Future<T>` | `Task.Run(func)` 返回 `Task<T>` |
| `future.get()` | `task.Result` / `task.Wait()` |
| `future.get(2, SECONDS)` | `task.Wait(2000)` |
| `future.cancel(true)` | `CancellationTokenSource.Cancel()`（协作式，见第 4 节） |
| `CompletableFuture.thenApply` | `ContinueWith`，或者 `await` 后面的代码 |
| `CompletableFuture.allOf` / `anyOf` | `Task.WhenAll` / `Task.WhenAny` |
| `CompletableFuture.completedFuture(x)` | `Task.FromResult(x)` |
| 手动 `new CompletableFuture<>()` + `complete(x)` | `TaskCompletionSource<T>` + `SetResult(x)` |

C# 不需要自己建 `ExecutorService`：**全局只有一个线程池**，Task 默认就跑在它上面。

## 2. 创建 Task 🟢

### 2.1 创建方式总表

| 方式 | 在线程池上执行？ | 现状 | 典型用途 |
|---|---|---|---|
| **`Task.Run(...)`** | 是 | ✅ **首选** | CPU 密集的工作 |
| `new Task(...)` + `Start()` | 是（冷启动，必须调 Start） | 少用 | 先创建，稍后再决定何时启动 |
| `Task.Factory.StartNew(...)` | 是 | 需要特殊选项时 | 要 state 参数、`TaskCreationOptions` |
| `Task.Factory.StartNew(..., LongRunning)` | **否，单独新建线程** | 偶尔 | 长时间运行的循环，不占线程池 |
| `async` 方法 | 第一个 await 前在调用方线程同步执行 | ✅ 日常 | 见第 13 章 |
| `Task.FromResult(x)`、`Task.CompletedTask` | 否，创建时就已完成 | 常用 | 签名要返回 Task，但结果已经有了（缓存、测试） |
| `Task.FromException(ex)`、`Task.FromCanceled(token)` | 否，创建时就已失败 / 取消 | 偶尔 | 同上，表示失败或取消 |
| `TaskCompletionSource<T>` | 否，你调 `SetResult` 时才完成 | 常用 | 把回调、事件包装成可以 await 的 Task |
| `ContinueWith`、`WhenAll`、`WhenAny`、`Task.Delay` | 视情况 | 常用 | 由其他 Task 组合而来 |
| `Task.Factory.FromAsync(BeginXxx, EndXxx, ...)` | 否 | 了解 | 把老的 APM 写法包装成 Task（见第 13 章） |

**只有前四种会把你的代码放到线程上执行**，其他几种本身不占用线程。

```csharp
var t1 = new Task<int>(() => 1); t1.Start();
var t2 = Task.Factory.StartNew(() => 2);
var t3 = Task.Run(() => 3);
Task<int> t4 = GetAsync();                        // async 方法：编译器生成 Task
static async Task<int> GetAsync() { await Task.Delay(10); return 4; }
Task<int> t5 = Task.FromResult(5);
Task t6 = Task.CompletedTask;
Task<int> t7 = Task.FromException<int>(new InvalidOperationException("x"));
Task<int> t8 = Task.FromCanceled<int>(new CancellationToken(true));

// TaskCompletionSource：用定时器在 100ms 后手动完成它
var tcs = new TaskCompletionSource<int>();
using var timer = new Timer(_ => tcs.TrySetResult(9), null, 100, Timeout.Infinite);
Task<int> t9 = tcs.Task;

Console.WriteLine($"t1={await t1} t2={await t2} t3={await t3} t4={await t4} t5={await t5}");
Console.WriteLine($"t6={t6.Status} t7={t7.Status} t8={t8.Status}");
Console.WriteLine($"t9 创建后 Status = {t9.Status}");
Console.WriteLine($"t9={await t9}");

Task<int> t10 = t3.ContinueWith(t => t.Result * 10);
int[] t11 = await Task.WhenAll(t1, t2, t3);
Console.WriteLine($"t10={await t10} WhenAll=[{string.Join(",", t11)}]");
```
```
t1=1 t2=2 t3=3 t4=4 t5=5
t6=RanToCompletion t7=Faulted t8=Canceled
t9 创建后 Status = WaitingForActivation
t9=9
t10=30 WhenAll=[1,2,3]
```

- 在同步方法里要返回 Task 时，用 `Task.FromResult` / `Task.CompletedTask`，**不要**写 `Task.Run(() => x)`，否则白白占一个线程池线程。
- `TaskCompletionSource` 是把"非 Task 的异步"（事件、回调、定时器）接入 `await` 的通用工具，第 8 节的定时器就用到它。

### 2.2 Task.Run：首选写法 🟢

`Task.Run` 能接收四种委托（[05-task-run.cs](../advanced/05-task-run.cs)）：

| 传入的 lambda | 委托类型 | 返回 |
|---|---|---|
| `() => { ... }` | `Action` | `Task` |
| `() => 值` | `Func<TResult>` | `Task<TResult>` |
| `async () => { await ...; }` | `Func<Task>` | `Task` |
| `async () => { await ...; return 值; }` | `Func<Task<TResult>>` | `Task<TResult>`（自动解包，不是 `Task<Task<T>>`） |

```csharp
Task t1 = Task.Run(() => Console.WriteLine("Action"));
Task<int> t2 = Task.Run(() => DoJob(123));       // DoJob: Thread.Sleep(1000); return n + 100;
Task<int> t3 = Task.Run(() => DoJob(456));
// Task.Run(DoJob, 456);                          // ❌ Task.Run 没有 state 重载，只能用闭包传参
Task t4 = Task.Run(async () => { await Task.Delay(500); Console.WriteLine("Func<Task> done"); });
Task<int> t5 = Task.Run(async () => { await Task.Delay(500); return 999; });

await Task.WhenAll(t1, t4);
int[] results = await Task.WhenAll(t2, t3, t5);   // 结果顺序和传入顺序一致
Console.WriteLine($"results = {string.Join(", ", results)}");
```
```
Action
Func<Task> done
results = 223, 556, 999
```

### 2.3 new Task + Start、Task.Factory.StartNew

```csharp
// new Task：冷启动，不调 Start 永远不会执行
Task task = new Task(() => DoJob(123));             // 闭包传参
task.Start();
Task task2 = new Task(state => DoJob(state), 456);  // Action<object?> + state
task2.Start();
var task3 = new Task<int>(state => state is int n ? n * n : 0, 9);
task3.Start();
Task.WaitAll(task, task2);
Console.WriteLine($"task3.Result = {task3.Result}");
```
```
main thread waiting...
123
456
do job 456 completed
do job 123 completed
task3.Result = 81
main thread end
```

`Task.Factory.StartNew` 创建并立即启动，比 `Task.Run` 多了 state 参数和各种选项。**日常用 `Task.Run`**，原因（[05-task-factory-startnew.cs](../advanced/05-task-factory-startnew.cs) 注释）：

1. 传 async lambda 会得到 `Task<Task>`，要再 `.Unwrap()`；`Task.Run` 自动解包。
2. 默认用"当前" TaskScheduler，在 UI 程序里可能跑到 UI 线程上。

`Task.Run(x)` 基本等于 `Task.Factory.StartNew(x, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)` 再加自动 Unwrap。

### 2.4 state 参数：必须是 object? ⚠️

StartNew 和 `new Task` 的 state 重载只认这两种委托：

| 重载 | 返回 |
|---|---|
| `StartNew(Action<object?> action, object? state)` | `Task` |
| `StartNew(Func<object?, TResult> func, object? state)` | `Task<TResult>` |

所以方法参数要写成 `object?`，用之前先判断类型：

```csharp
Func<object?, int> job = DoJob;
Task<int> task  = Task.Factory.StartNew(() => job(123));   // Func<int>：闭包传参
Task<int> task2 = Task.Factory.StartNew(job, 456);         // Func<object?, int> + state
Task<int> sum   = Task.Factory.StartNew(Sum, 10);          // 直接传方法组

static int DoJob(object? obj)
{
    Console.WriteLine(obj);
    Thread.Sleep(1000);
    return obj is int number ? number + 100 : 0;           // 可能是 null 或不是 int
}
```
```
main thread waiting...
456
123
task.Result = 223, task2.Result = 556
0 + 1 + ... + 10 = 55
main thread end
```

传进去的 state 之后可以用 `task.AsyncState` 取回来（类型 `object?`），见 [05-task-many-tasks.cs](../advanced/05-task-many-tasks.cs)。

### 2.5 两个让人困惑的编译错误 ⚠️

**CS1593：lambda 参数个数和委托不匹配**

```csharp
Task t = Task.Run(x => Console.WriteLine(x));   // Task.Run 没有带参数的委托重载
```
```
error CS1593: Delegate 'Action' does not take 1 arguments
```

编译器挑了 `Task.Run(Action)` 去匹配，发现 `Action` 没有参数。`StartNew((a, b) => ..., 1)` 同理报 "Delegate 'Action' does not take 2 arguments"。

**CS1503：方法参数写成 int 而不是 object?**

```csharp
Task<int> t = Task.Factory.StartNew(DoJob, 456);
static int DoJob(int n) => n + 100;
```
```
error CS1503: Argument 1: cannot convert from 'method group' to 'System.Action'
error CS1503: Argument 2: cannot convert from 'int' to 'System.Threading.CancellationToken'
```

报错说的是 `Action` 和 `CancellationToken`，看起来和你的代码毫无关系。真实原因是：`DoJob(int)` 匹配不上 `Func<object?, int>`，编译器所有重载都试过了失败，最后拿 `StartNew(Action, CancellationToken)` 来报错。**看到这种迷惑报错，先检查方法参数是不是 `object?`**，或者改用闭包 `StartNew(() => DoJob(456))`。

### 2.6 Task.Run 会不会创建线程？🟡

`Task.Run` 本身不创建线程，只是把任务放进线程池的队列；线程池再根据负载决定要不要加线程。实测：

```csharp
Console.WriteLine($"启动时 ThreadPool.ThreadCount = {ThreadPool.ThreadCount}");
Task.Run(() => Print("Task.Run")).Wait();
Task.Factory.StartNew(() => Print("LongRunning"), TaskCreationOptions.LongRunning).Wait();
// 顺序执行 100 次 Task.Run，统计不同的线程 id
// 再同时提交 30 个 Thread.Sleep(2000) 的任务，每秒看一次 ThreadCount
```
```
启动时 ThreadPool.ThreadCount = 0
Main: 线程 1, 线程池线程 = False
Task.Run: 线程 4, 线程池线程 = True, 后台线程 = True
LongRunning: 线程 7, 线程池线程 = False, 后台线程 = True
第一次 Task.Run 之后 ThreadCount = 2
顺序执行 100 次 Task.Run，用到的线程数 = 3
CPU 核数 = 8, 最小工作线程数 = 8
阻塞前 ThreadCount = 3
1 秒后 ThreadCount = 9
2 秒后 ThreadCount = 11
3 秒后 ThreadCount = 12
```

| 现象 | 说明 |
|---|---|
| 启动时 `ThreadCount = 0` | 线程池是懒创建的 |
| 顺序 100 次只用了 3 个线程 | 有空闲线程就复用 |
| 30 个阻塞任务：3 → 9 → 11 → 12 | 先快速补到最小值（= CPU 核数 8），之后大约每 0.5 秒才加一个 |
| `LongRunning`：线程池线程 = False | 单独新建一个线程，不占线程池 |

打个比方：`Task.Run` 是"叫号排队"，线程池是"柜台"。排队不会增加柜台，只有柜台都忙不过来时，才慢慢加开。

**在线程池里阻塞很慢**（[05-task-many-tasks.cs](../advanced/05-task-many-tasks.cs)）：100 个任务各睡 100ms。

```
Thread.Sleep 版：1 到 100 的和 = 5050，耗时 1213ms
Task.Delay 版：1 到 100 的和 = 5050，耗时 106ms
```

`Thread.Sleep` 占着线程，100 个任务只能几个一组地跑；`await Task.Delay` 等待期间不占线程，100 个同时等。这就是"线程池饥饿"，原理见第 11 章，`await` 见第 13 章。

## 3. 等待任务：Wait、Result 🟢

### 3.1 .Result 和 Wait() 会阻塞

```csharp
var sw = Stopwatch.StartNew();
Task<int> t = Task.Run(() => { Thread.Sleep(1000); return 1; });
Console.WriteLine($"Task.Run 立即返回：{sw.ElapsedMilliseconds}ms");
int r = t.Result;
Console.WriteLine($"读 .Result 阻塞到任务完成：{sw.ElapsedMilliseconds}ms");
```
```
Task.Run 立即返回：1ms
读 .Result 阻塞到任务完成：1002ms
```

- 读了 `.Result` 就不需要再 `Wait()` / `WaitAll`，它自己会等。
- 在 `async` 方法里应该用 `await`，不要用 `.Result` / `.Wait()`（会阻塞线程，UI 程序里可能死锁，见第 13 章）。

### 3.2 Wait(int)、Wait(token)：取消的是"等待"，不是任务 ⚠️

```csharp
Task task2 = StartTicking(5);                     // 每秒打印一次，共 5 次
bool completed = task2.Wait(2500);
Console.WriteLine(completed ? "Wait(2500): completed" : "Wait(2500): timeout, still running in background");
task2.Wait();
```
```
  tick 1/5 at 17:23:36
  tick 2/5 at 17:23:37
  tick 3/5 at 17:23:38
Wait(2500): timeout, still running in background
  tick 4/5 at 17:23:39
  tick 5/5 at 17:23:40
```

`Wait(token)` 也一样（[05-task-wait-cancellation.cs](../advanced/05-task-wait-cancellation.cs)），token 取消后 Wait 抛 `OperationCanceledException`，任务照样跑完：

```
  tick 1/4 at 17:23:24
  tick 2/4 at 17:23:25
不等了，但任务还在跑：Status = Running
  tick 3/4 at 17:23:26
  tick 4/4 at 17:23:27
```

| 写法 | 任务完成 | 超时 | token 取消 |
|---|---|---|---|
| `Wait()` | 返回 | — | — |
| `Wait(int ms)` | 返回 `true` | 返回 `false`，**不抛异常** | — |
| `Wait(token)` | 返回 | — | 抛 `OperationCanceledException` |
| `Wait(int, token)` | `true` | `false` | 抛异常（哪个先到按哪个） |

Java 的 `future.get(timeout)` 超时也不会停止任务，道理一样。**要让任务真正停下来，必须把 token 传进任务内部去检查**。

## 4. 取消：CancellationToken 🟢

### 4.1 协作式取消

C# 没有"强行杀掉一个任务"的办法（Java 的 `Thread.stop` 早已废弃，`future.cancel(true)` 也只是发中断信号）。取消靠双方配合：

| 角色 | 类型 | 常用成员 |
|---|---|---|
| 发令方 | `CancellationTokenSource` | `Cancel()`、`CancelAfter(ms)`、构造函数 `new CancellationTokenSource(ms)`、`Token` |
| 接收方 | `CancellationToken` | `IsCancellationRequested`、`ThrowIfCancellationRequested()` |

任务内部有两种退出方式（[05-task-cancellation.cs](../advanced/05-task-cancellation.cs)）：

```csharp
using var cts = new CancellationTokenSource();
CancellationToken token = cts.Token;

// 方式 1：抛异常退出
Task task = Task.Factory.StartNew(state =>
{
    if (state is not int count) return;
    for (int i = 0; i < count; i++)
    {
        token.ThrowIfCancellationRequested();   // 检查点：不检查就永远不会停
        Console.WriteLine($"current time: {DateTime.Now:HH:mm:ss}");
        Thread.Sleep(1000);
    }
}, 10, token);                                   // 同一个 token 也传给 StartNew
cts.CancelAfter(3000);
try { task.Wait(); }
catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
{
    Console.WriteLine($"任务被取消，Status = {task.Status}");
}

// 方式 2：安静退出
using var cts2 = new CancellationTokenSource(1500);
Task quiet = Task.Run(() =>
{
    while (!cts2.Token.IsCancellationRequested) { Console.WriteLine("working..."); Thread.Sleep(500); }
    Console.WriteLine("收到取消通知，退出");
});
quiet.Wait();
Console.WriteLine($"Status = {quiet.Status}");
```
```
current time: 17:23:09
current time: 17:23:10
current time: 17:23:11
任务被取消，Status = Canceled
--------------------------
working...
working...
working...
收到取消通知，退出
Status = RanToCompletion
```

安静退出还可以返回"取消前算到的部分结果"（[05-task-cancel-partial-result.cs](../advanced/05-task-cancel-partial-result.cs)）：

```
sum = 0
sum = 1
sum = 3
Status = RanToCompletion, 取消前算到的 sum = 3
```

### 4.2 token 要不要传给 Task.Run？⚠️

传给任务内部的 token 负责"让计算停下来"；传给 `Task.Run` / `StartNew` 的 token 管两件事：

```csharp
// 1. 抛 OperationCanceledException，但没把 token 传给 Task.Run
cts.Cancel();
Task noToken = Task.Run(() => cts.Token.ThrowIfCancellationRequested());
// 2. 同样的代码，token 也传给 Task.Run
Task withToken = Task.Run(() => { Thread.Sleep(200); cts2.Token.ThrowIfCancellationRequested(); }, cts2.Token);
```
```
没传 token：Status = Faulted
Wait 抛出 AggregateException，内部是 TaskCanceledException
传了 token：Status = Canceled
await 抛出 TaskCanceledException
```

启动前就已经取消的 token 传给 `Task.Run`，任务根本不会执行（[05-task-cancellation.cs](../advanced/05-task-cancellation.cs)）：

```csharp
cts3.Cancel();
Task skipped = Task.Run(() => Console.WriteLine("不会打印"), cts3.Token);
try { skipped.Wait(); } catch (AggregateException) { }
Console.WriteLine($"启动前已取消：Status = {skipped.Status}");
```
```
启动前已取消：Status = Canceled
```

| | 不传给 Task.Run | 传给 Task.Run |
|---|---|---|
| 任务里抛 `OperationCanceledException` | `Faulted`（当成普通异常） | `Canceled` |
| 任务开始前 token 已取消 | 照样执行 | **根本不执行**，直接 `Canceled` |
| 运行中途取消 | 都要靠任务内部检查 | 同左 |

结论：**同一个 token，任务内部检查一份，`Task.Run` 第二个参数也传一份。**

## 5. 延续：ContinueWith 🟡

前一个任务完成后自动执行下一个，相当于 Java 的 `thenApply` / `thenAccept`（[05-task-continuation.cs](../advanced/05-task-continuation.cs)）：

```csharp
Task<int> taskA = Task.Run(() => { Console.WriteLine("taskA start"); Thread.Sleep(1000); Console.WriteLine("taskA end"); return 123; });
Task taskB = taskA.ContinueWith(t =>                  // t 就是 taskA
{
    Console.WriteLine($"taskA status: {t.Status}");
    Console.WriteLine($"taskA result: {t.Result}");    // t 已完成，读 Result 不会阻塞
});
Console.WriteLine("main thread continues...");
taskB.Wait();

int final = await Task.Run(() => 2)
    .ContinueWith(t => t.Result * 10)
    .ContinueWith(t => t.Result + 1);
Console.WriteLine($"链式 ContinueWith: {final}");
```
```
main thread start
main thread continues...
taskA start
taskA end
taskB (continuation) start
taskA status: RanToCompletion
taskA result: 123
taskB end
--------------------------
链式 ContinueWith: 21
```

用 `TaskContinuationOptions` 控制什么情况下执行：

```csharp
Task a = ok.ContinueWith(t => ..., TaskContinuationOptions.OnlyOnRanToCompletion);
Task b = ok.ContinueWith(t => ..., TaskContinuationOptions.OnlyOnFaulted);    // ok 成功了，b 不执行
Task c = bad.ContinueWith(t => Console.WriteLine($"OnlyOnFaulted: {t.Exception!.InnerException!.Message}"),
                          TaskContinuationOptions.OnlyOnFaulted);
Task d = bad.ContinueWith(t => Console.WriteLine($"无选项也会执行：前驱 Status = {t.Status}"));
```
```
OnlyOnRanToCompletion: 42
无选项也会执行：前驱 Status = Faulted
OnlyOnFaulted: boom
条件不满足的延续任务：Status = Canceled
```

| 选项 | 什么时候执行 |
|---|---|
| 不传 | 前驱不管成功、失败、取消，都执行 |
| `OnlyOnRanToCompletion` | 前驱成功 |
| `OnlyOnFaulted` | 前驱抛异常 |
| `OnlyOnCanceled` | 前驱被取消 |
| `NotOnFaulted` / `NotOnCanceled` / `NotOnRanToCompletion` | 反过来 |

- ⚠️ 条件不满足的延续任务会变成 `Canceled`，`await` 它会抛 `TaskCanceledException`。
- ⚠️ 不传选项时，前驱失败了你读 `t.Result` 会抛 `AggregateException`。
- 日常开发**用 `await` 代替 `ContinueWith`**：`await` 后面的代码就是延续，异常也能直接 try/catch。`ContinueWith` 主要在老代码和不能用 async 的地方见到。

## 6. 等待多个任务 🟢

### 6.1 WaitAll vs WhenAll

| | `Task.WaitAll` | `Task.WhenAll` |
|---|---|---|
| 等待方式 | **同步阻塞** | **异步**，返回 Task，配合 `await` |
| 返回值 | `void`（带超时的重载返回 `bool`） | `Task`，或者 `Task<TResult[]>` |
| 拿结果 | 再逐个读 `t1.Result`、`t2.Result` | `await` 直接得到结果数组，顺序和传入一致 |
| 异常 | 抛 `AggregateException`，包含**所有**异常 | `await` 抛**第一个**异常本身；全部异常在返回的 Task 的 `.Exception` 里 |
| 超时 / 取消 | `WaitAll(tasks, 5000)`、`WaitAll(tasks, token)` | `await Task.WhenAll(...).WaitAsync(TimeSpan.FromSeconds(5))` |
| 用在哪里 | 同步方法，比如 `void Main` | `async` 方法里 |

```csharp
Task a = Task.Run(() => throw new InvalidOperationException("A"));
Task b = Task.Run(() => throw new ArgumentException("B"));
try { Task.WaitAll(a, b); }
catch (AggregateException ex) { /* 打印 ex.InnerExceptions */ }

Task all = Task.WhenAll(a, b);
try { await all; }
catch (Exception ex) { /* 打印 ex 和 all.Exception.InnerExceptions.Count */ }
```
```
WaitAll: AggregateException, InnerExceptions.Count = 2
  InvalidOperationException: A
  ArgumentException: B
await WhenAll: InvalidOperationException: A
  all.Exception.InnerExceptions.Count = 2
```

**原则**：在 `async` 方法里一律 `await Task.WhenAll`；只有在不能用 async 的同步代码里才用 `Task.WaitAll`。`WaitAll` 让线程干等，在线程池线程上阻塞会导致线程池饥饿，在 UI 线程上还可能死锁（第 13 章）。

### 6.2 WaitAny vs WhenAny

```csharp
Task<int> slow = Task.Run(async () => { await Task.Delay(1000); return 1; });
Task<int> fast = Task.Run(async () => { await Task.Delay(100); return 2; });
int index = Task.WaitAny(slow, fast);             // 阻塞，返回最先完成的下标
Task<int> first = await Task.WhenAny(slow, fast); // 异步，返回最先完成的那个 Task

// 自己实现超时
Task work = Task.Delay(5000);
if (await Task.WhenAny(work, Task.Delay(300)) == work) Console.WriteLine("完成");
else Console.WriteLine("超时了");

// .NET 6+ 更简单：WaitAsync
try { await Task.Delay(5000).WaitAsync(TimeSpan.FromMilliseconds(300)); }
catch (TimeoutException) { Console.WriteLine("WaitAsync: TimeoutException"); }
```
```
WaitAny 返回下标 1
WhenAny 返回的任务结果 = 2
超时了
WaitAsync: TimeoutException
```

⚠️ 和 `Wait(int)` 一样，超时只是"不等了"，原来的任务并没有停。

## 7. 数据并行：Parallel 🟢

`Parallel` 类把循环自动分到多个线程池线程上，适合 **CPU 密集**的计算。相当于 Java 的 `ForkJoinPool` / `parallelStream()`。

### 7.1 Parallel.Invoke：同时执行几个方法

```csharp
Method1(); Method2(); Method3();                  // 分别睡 0.5、1、1.5 秒
Console.WriteLine($"串行执行耗时：{stopwatch.ElapsedMilliseconds}ms");
stopwatch.Restart();
Parallel.Invoke(Method1, Method2, Method3);
Console.WriteLine($"并行执行耗时：{stopwatch.ElapsedMilliseconds}ms");
```
```
main thread start
Method1 completed on thread 1
Method2 completed on thread 1
Method3 completed on thread 1
串行执行耗时：3016ms
Method1 completed on thread 1
Method2 completed on thread 4
Method3 completed on thread 6
并行执行耗时：1506ms
main thread end
```

两个关键点：

- **会阻塞**：全部执行完才返回。
- **调用线程也参与执行**：上面 Method1 就是在主线程（线程 1）上跑的。

不想阻塞调用方时，外面包一层 `Task.Run`（[07-parallel-in-task-run.cs](../advanced/07-parallel-in-task-run.cs)）：

```csharp
Task task = Task.Run(() => Parallel.Invoke(() => Square(2), () => Cube(3)));
Console.WriteLine("main thread is doing other work...");
await task;
```
```
main thread start
main thread is doing other work...
2 * 2 = 4
3 * 3 * 3 = 27
main thread end
```

如果 `Task.Run` 之后马上 `task.Wait()`，就和直接调用没区别，还多占一个线程。典型用途是 UI 程序里 `await Task.Run(() => Parallel.For(...))`，界面不卡。

### 7.2 Parallel.For / ForEach

```csharp
Parallel.For(0, 100, i => Compute(i));             // 包含 0，不包含 100
Parallel.ForEach(items, item => Handle(item));
```

- 执行顺序不固定。
- 多个线程同时写的集合要用 `ConcurrentBag` / `ConcurrentQueue` / `ConcurrentDictionary`；只读的普通 `List` 没问题。
- 实测 `Parallel.For(0, 100, ...)` 每次睡 10ms，用到了 9 个线程，包含调用线程。

### 7.3 Break vs Stop 🟡

```csharp
ParallelLoopResult result = Parallel.For(1, 41, (i, state) =>
{
    if (i == 5) { state.Break(); return; }        // 或 state.Stop()；它们只设标志，要自己 return
    Thread.Sleep(100);
    executed.Add(i);
});
```
```
Break: IsCompleted = False
Break: LowestBreakIteration = 5
Break: 执行了 1,2,3,4,6,7,11,12,16,17,21,22,26,27,31,32,36,37

Stop: IsCompleted = False
Stop: LowestBreakIteration = null
Stop: 执行了 1,2,3,6,7,11,12,16,17,21,22,26,27,31,32,36,37
```

| | `Break()` | `Stop()` |
|---|---|---|
| 比当前编号小的迭代 | **保证执行完**（上面 1~4 都在） | 不保证（上面 4 没执行） |
| 还没开始的 | 编号大的不再启动 | 全部不再启动 |
| `LowestBreakIteration` | 调用 Break 的编号（`long?`） | `null` |
| 类比 | 普通 `for` 里的 `break` | "全部尽快停下" |

两者都不会打断**已经开始**的迭代，所以总会多执行一些编号更大的（上面 6、7、11、12…是各线程已经领到的）。

### 7.4 线程本地变量：localInit / body / localFinally 🟡

并行求和，直接 `sum += i` 会丢数据（读-加-写不是原子的）。正确又高效的写法是每个线程先算自己的小计，最后汇总（[07-parallel-for-thread-local.cs](../advanced/07-parallel-for-thread-local.cs)）：

```csharp
long sum = 0;
Parallel.For<long>(1, 1001,
    () => 0L,                                          // ① localInit：每个线程开始时调一次
    (i, loop, subtotal) => subtotal + i,               // ② body：只改本线程的小计，不用加锁
    subtotal =>                                        // ③ localFinally：每个线程结束时调一次
    {
        Console.WriteLine($"线程 {Environment.CurrentManagedThreadId} 的小计: {subtotal}");
        Interlocked.Add(ref sum, subtotal);            // 多线程写 sum，用原子操作
    });
Console.WriteLine($"1 + 2 + ... + 1000 = {sum}");
```
```
线程 11 的小计: 29221
线程 4 的小计: 126
线程 6 的小计: 160361
...（共 9 行，每次运行不同）
1 + 2 + ... + 1000 = 500500
```

| 写法 | 结果 |
|---|---|
| `Parallel.For(..., i => sum += i)` | ❌ 不是原子操作，会丢数据 |
| `Parallel.For(..., i => Interlocked.Add(ref sum, i))` | ⚠️ 正确，但 1000 次都在抢同一个变量 |
| 线程本地小计 + `localFinally` | ✅ 只同步"线程数"次 |

`ref sum`：把变量本身（的地址）传进去，而不是副本，`Interlocked.Add` 才能改到它（ref 见第 2 章，`Interlocked` 见第 14 章）。

`Parallel.ForEach` 也有同样的重载，还能拿到索引（[07-parallel-foreach.cs](../advanced/07-parallel-foreach.cs)）：

```csharp
var options = new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = cts.Token };
long totalBalance = 0;
Parallel.ForEach<Person, int>(persons, options,
    () => 0,
    (person, loop, index, balance) =>
    {
        int random = Random.Shared.Next(10, 100);      // 10~99
        person.Balance = random;
        Console.WriteLine($"{index}: {person.Name}: {person.Balance}  (线程 {Environment.CurrentManagedThreadId})");
        return balance + random;
    },
    balance => Interlocked.Add(ref totalBalance, balance));
```
```
0: 刘备: 82  (线程 1)
2: 张飞: 72  (线程 1)
1: 关羽: 59  (线程 6)
IsCompleted = True
totalBalance = 213, 验证: 213
```

### 7.5 ParallelOptions

| 属性 | 作用 |
|---|---|
| `MaxDegreeOfParallelism` | 最多同时用几个线程（默认不限制，由线程池决定）。上面设成 2，只用了线程 1 和 6 |
| `CancellationToken` | 取消整个循环，`Parallel.For` 抛 `OperationCanceledException` |

```csharp
var options = new ParallelOptions { CancellationToken = cts.Token, MaxDegreeOfParallelism = 2 };
try
{
    Parallel.For(0, 100, options, i =>
    {
        if (Interlocked.Increment(ref count) == 5) cts.Cancel();
        Thread.Sleep(50);
    });
}
catch (OperationCanceledException ex) { Console.WriteLine($"Parallel.For 被取消：{ex.GetType().Name}，执行了 {count} 次"); }
```
```
Parallel.For 被取消：OperationCanceledException，执行了 6 次
```

### 7.6 PLINQ：AsParallel

LINQ 后面加一个 `.AsParallel()` 就并行执行，相当于 Java 的 `list.parallelStream()`：

```csharp
long plinq = Enumerable.Range(1, 1000).AsParallel().Sum(x => (long)x);   // 500500
int threads = Enumerable.Range(1, 100).AsParallel()
    .Select(x => { Thread.Sleep(10); return Environment.CurrentManagedThreadId; })
    .Distinct().Count();
Enumerable.Range(1, 10).AsParallel().ForAll(x => Console.Write($"{x} "));
```
```
PLINQ 用到的线程数 = 8
9 7 3 5 1 2 6 8 10 4
```

- 默认**不保证顺序**；需要保持原顺序时加 `.AsOrdered()`。
- 只对 CPU 密集、数据量大的计算有意义；数据量小时，拆分和汇总的开销比收益还大。

### 7.7 Random 的线程安全 ⚠️

```csharp
int random = Random.Shared.Next(10, 100);   // 10~99：包含下限，不包含上限
```

- `Next(min, max)` **不包含上界**：实测 `Next(1, 4)` 跑 1000 次只出现过 `1,2,3`。和 Java 的 `nextInt(bound)` 一样。
- `new Random()` 的实例方法**不是线程安全的**（官方文档的说明），多个线程共用一个实例可能把内部状态写坏。本机用 8 个线程跑一千万次没有复现出错误，但不能依赖这一点。
- 多线程里用 **`Random.Shared`**（.NET 6+，线程安全）。
- 老资料里"用 `Guid.NewGuid().GetHashCode()` 当种子，避免多个 Random 同一时刻创建出相同序列"是 .NET Framework 时代的问题，现代 .NET 不需要。

## 8. 定时器 🟢

.NET 常用的有三种（还有 WinForms / WPF 的 UI 定时器，这里不讲），回调都在**线程池线程**上执行。

### 8.1 System.Threading.Timer

```csharp
// 参数：回调、state、dueTime（首次延迟 ms）、period（间隔 ms）
var timer = new Timer(state => Tick(), null, dueTime: 1000, period: 3000);
```

⚠️ **最常见的坑：Main 先退出了**。Timer 没有 `Wait()`，也不能 `await`：

```csharp
Console.WriteLine("main start");
var timer = new Timer(_ => Console.WriteLine("tick"), null, 1000, 1000);
Console.WriteLine("main end");
```
```
main start
main end
```

线程池线程都是后台线程，Main 返回，进程就结束了，回调一次都没执行。必须自己准备一个"结束信号"（[10-threading-timer.cs](../advanced/10-threading-timer.cs)）：

```csharp
// 写法 1：ManualResetEventSlim + Wait()，阻塞主线程
using var done = new ManualResetEventSlim(false);
using (Timer timer = new Timer(state =>
{
    if (state is Person p)
    {
        Console.WriteLine($"[Wait]  balance = {p.Balance}");
        p.Balance -= 20;
        if (p.Balance == 0) done.Set();     // 通知主线程（别用 Environment.Exit(0)，会直接杀进程）
    }
}, person, 1000, 3000))
{
    done.Wait();
}                                           // using：Dispose 后定时器停止

// 写法 2：TaskCompletionSource + await，不阻塞
var tcs = new TaskCompletionSource();
using (Timer timer2 = new Timer(_ =>
{
    Console.WriteLine($"[await] balance = {person2.Balance}");
    person2.Balance -= 20;
    if (person2.Balance == 0) tcs.TrySetResult();
}, null, 500, 1000))
{
    await tcs.Task;
}
```
```
main thread start
[Wait]  balance = 100
[Wait]  balance = 80
[Wait]  balance = 60
[Wait]  balance = 40
[Wait]  balance = 20
--------------------------
[await] balance = 60
[await] balance = 40
[await] balance = 20
main thread end
```

另外两个坑：

- **回调会重叠**：定时器到点就调，不管上一次回调结束没有。实测间隔 100ms、回调耗时 500ms，最多 6 个回调同时在跑。回调里改共享数据要考虑线程安全。
- **回调里的异常会让进程崩溃**：

```
Unhandled exception. System.InvalidOperationException: Timer 回调里的异常
   at Program.<>c.<<Main>$>b__0_0(Object _) in .../k-threading-timer-exception.cs:line 1
   ...
   at System.Threading.TimerQueueTimer.Fire(Boolean isThreadPool)
```

- ⚠️ 要把 Timer 存在变量里并在用完时 Dispose。只 `new` 出来不保存引用的 Timer 可能被 GC 回收，然后就不触发了。

### 8.2 System.Timers.Timer

基于事件的写法（[10-timers-timer.cs](../advanced/10-timers-timer.cs)）：

```csharp
using System.Timers;
using System.Timers.Timer timer = new System.Timers.Timer(1000);   // 写全称：System.Threading 里也有 Timer
timer.AutoReset = true;                  // true：每隔 1 秒触发；false：只触发一次
timer.Elapsed += TimerElapsed;           // 事件，可以订阅多个
timer.Start();
await Task.Delay(5500);                  // 一样要让 Main 等着
timer.Stop();

static void TimerElapsed(object? sender, ElapsedEventArgs e)
{
    Console.WriteLine($"{e.SignalTime:HH:mm:ss} balance = {person.Balance}");
    person.Balance -= 20;
}
```
```
main thread start
17:24:13 balance = 100
17:24:14 balance = 80
17:24:15 balance = 60
17:24:16 balance = 40
17:24:17 balance = 20
main thread end
```

⚠️ **Elapsed 里的异常被吞掉**，和 Threading.Timer 正好相反：

```csharp
timer.Elapsed += (s, e) => { n++; Console.WriteLine($"Elapsed {n}，线程池线程 = {Thread.CurrentThread.IsThreadPoolThread}"); throw new InvalidOperationException("..."); };
```
```
Elapsed 1，线程池线程 = True
Elapsed 2，线程池线程 = True
Elapsed 3，线程池线程 = True
异常被吞掉了，进程还活着
```

不会崩溃，但出了错你也看不到。Elapsed 里一定要自己 try/catch 并记日志。

### 8.3 PeriodicTimer（推荐）🟢

.NET 6 新增，专为 async 设计：不是定时器"推着你"调回调，而是你在自己的循环里 `await` 下一次触发（[10-periodic-timer.cs](../advanced/10-periodic-timer.cs)）：

```csharp
await Task.Delay(1000);                                     // 第一次延迟 1 秒
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
do
{
    Console.WriteLine($"{DateTime.Now:HH:mm:ss} balance = {person.Balance}");
    person.Balance -= 20;
}
while (person.Balance > 0 && await timer.WaitForNextTickAsync());
```
```
main thread start
17:23:52 balance = 100
17:23:53 balance = 80
17:23:54 balance = 60
17:23:55 balance = 40
17:23:56 balance = 20
main thread end
```

- 代码从上往下执行，**不会重叠**：实测间隔 100ms、循环体耗时 500ms，2 秒内只执行了 4 次。
- 不需要额外的结束信号，`await` 本身就让 Main 等着。
- 异常就在你的循环里抛出，正常 try/catch。
- 取消：`WaitForNextTickAsync(token)`，取消时抛 `OperationCanceledException`；`timer.Dispose()` 之后返回 `false`，循环结束。

### 8.4 三种定时器对比

| | `System.Threading.Timer` | `System.Timers.Timer` | `PeriodicTimer` |
|---|---|---|---|
| 写法 | 构造函数传回调 | `Elapsed` 事件 | `await WaitForNextTickAsync()` 循环 |
| 执行线程 | 线程池 | 线程池 | `await` 之后的线程池线程 |
| 启停 | `Change(dueTime, period)`、`Dispose` | `Start()` / `Stop()` / `Enabled` | `Dispose` / 取消 token |
| 只触发一次 | `period = Timeout.Infinite` | `AutoReset = false` | 循环一次就退出 |
| 上一次没执行完 | **重叠执行** | **重叠执行** | 不重叠 |
| 回调里的异常 | **进程崩溃** | **被吞掉** | 在循环里抛出，正常 catch |
| 等它结束 | 自己用信号 / TCS | 自己等 | `await` 即可 |
| Java 对应 | `ScheduledExecutorService.scheduleAtFixedRate` | `java.util.Timer` | 无直接对应 |
| 推荐 | 老代码、简单回调 | 老代码 | ✅ **新代码首选** |

服务端程序里的后台定时任务，通常写成 ASP.NET Core 的 `BackgroundService` + `PeriodicTimer`。

## 9. 怎么选

| 场景 | 选择 |
|---|---|
| 等待 I/O（文件、网络、数据库） | `async` / `await`，**不需要开线程**（第 13 章） |
| CPU 密集的计算，不想阻塞当前线程 | `Task.Run` |
| 一批数据要并行计算 | `Parallel.For` / `ForEach`、PLINQ |
| 几个独立方法同时跑 | `Parallel.Invoke`，或多个 `Task.Run` + `WhenAll` |
| 长时间运行的后台循环 | `Task.Factory.StartNew(..., LongRunning)`，或 `new Thread`（第 11 章） |
| 同步方法要返回 Task，结果已有 | `Task.FromResult` / `Task.CompletedTask` |
| 把回调、事件变成可以 await 的 Task | `TaskCompletionSource` |
| 定时执行 | `PeriodicTimer`；不能用 async 时用 `System.Threading.Timer` |

和 Java 的整体对照：

| Java | C# |
|---|---|
| `ExecutorService` / `Executors.newFixedThreadPool` | 全局线程池，`Task.Run` 直接用 |
| `Future<T>` | `Task<T>` |
| `CompletableFuture` | `Task<T>` + `ContinueWith` / `await`、`TaskCompletionSource` |
| `ForkJoinPool` / `RecursiveTask` | `Parallel.For` / `Parallel.Invoke` |
| `parallelStream()` | PLINQ `AsParallel()` |
| `ScheduledExecutorService` | `System.Threading.Timer`、`PeriodicTimer` |
| `Thread.interrupt()` / `isInterrupted()` | `CancellationToken`（协作式） |

## 10. 小结

1. 后台执行代码首选 `Task.Run`；它用的是线程池，本身不创建线程。只有 `LongRunning` 和 `new Thread` 会单独新建线程。
2. `Task.Run` 没有 state 重载，用闭包传参；`StartNew` / `new Task` 的 state 参数是 `object?`，方法参数写错会出现 CS1503 这种指向 `Action`、`CancellationToken` 的迷惑报错。
3. `.Result`、`Wait()`、`WaitAll` 都会阻塞；`Wait(ms)` / `Wait(token)` / `WhenAny` 超时只是"不等了"，任务还在跑。
4. 取消是协作式的：任务内部检查 token，同时把 token 传给 `Task.Run`，才能得到 `Canceled` 状态，并在启动前取消时直接跳过。
5. async 代码里用 `await Task.WhenAll` / `WhenAny`，不用 `WaitAll` / `WaitAny`；`await` 只抛第一个异常，全部异常在 `task.Exception` 里。
6. `Parallel` 会阻塞并且调用线程也参与；并行累加用"线程本地小计 + `Interlocked.Add(ref …)`"；多线程随机数用 `Random.Shared`，`Next` 不包含上界。
7. 定时器回调都在线程池上，Main 不等就直接退出；新代码优先用 `PeriodicTimer`，注意 Threading.Timer 回调异常会崩溃、Timers.Timer 会吞异常。

---

上一章：[线程与线程池 🟡](11-threads.md) · [目录](../README.md) · 下一章：[async/await 与异常 🟢](13-async-await.md)
