# 第 13 章 async/await 与异常 🟢

> 本章练习：[06-apm-begin-read.cs](../advanced/06-apm-begin-read.cs)、[06-async-file-read.cs](../advanced/06-async-file-read.cs)、[08-async-await-basics.cs](../advanced/08-async-await-basics.cs)、[08-async-cancellation.cs](../advanced/08-async-cancellation.cs)、[08-async-whenall.cs](../advanced/08-async-whenall.cs)、[08-httpclient.cs](../advanced/08-httpclient.cs)、[09-thread-exception.cs](../advanced/09-thread-exception.cs)、[09-task-exception.cs](../advanced/09-task-exception.cs)、[09-async-method-exception.cs](../advanced/09-async-method-exception.cs)、[09-global-exception-handlers.cs](../advanced/09-global-exception-handlers.cs)

> 写代码时你会遇到这些问题：方法标了 `async`，为什么界面还是卡了一秒？`async void` 里抛的异常为什么 try/catch 不住，进程直接崩了？同样是等一个失败的任务，`.Wait()` 抛的是 `AggregateException`，`await` 抛的却是原始异常？`Task.WhenAll` 里三个任务都失败了，为什么只 catch 到一个？程序跑完什么都没输出？
> 本章讲现代 C# 写异步代码的主力：`async` / `await`。它不开线程，却能让一个线程同时"等"成百上千个 I/O 操作。Java 里最接近的是 `CompletableFuture` 链式调用和 JDK 21 的虚拟线程。后半章讲线程、Task、async 方法里的异常各自怎么传递、怎么兜底。Task 本身的创建、等待、取消见[第 12 章](12-task-parallel.md)。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。线程 id 每次运行可能不同。

## 1. 为什么要异步 🟢

后端程序大部分时间在**等**：等数据库、等 HTTP 接口、等读文件。用同步写法（Java 1.6 的 `InputStream.read()`、JDBC），线程在等待期间什么也不干，但一直被占着。

`await` 一个 I/O 操作时，**等待期间不占用任何线程**；操作完成后，再从线程池里取一个线程接着执行 `await` 后面的代码。

```csharp
// 1000 个"I/O 等待"同时进行
Console.WriteLine($"开始前 ThreadCount = {ThreadPool.ThreadCount}");
var tasks = Enumerable.Range(0, 1000).Select(async _ => { await Task.Delay(1000); }).ToArray();
await Task.Delay(500);
Console.WriteLine($"1000 个 await Task.Delay(1000) 进行中：ThreadCount = {ThreadPool.ThreadCount}");
await Task.WhenAll(tasks);
Console.WriteLine($"全部完成，耗时 {sw.ElapsedMilliseconds}ms");
```
```
开始前 ThreadCount = 0
1000 个 await Task.Delay(1000) 进行中：ThreadCount = 2
全部完成，耗时 1010ms
```

1000 个同时在等，线程池只有 2 个线程。换成 `Thread.Sleep` 的版本就要几个一组地排队（第 12 章实测：100 个任务 1213ms vs 106ms）。

| | 同步阻塞 | `async` / `await` |
|---|---|---|
| 等待期间线程 | 被占着，干等 | 还回线程池，去干别的 |
| 1000 个并发请求 | 需要约 1000 个线程 | 几个线程就够 |
| 适合 | 简单脚本 | 服务端、UI 程序、任何 I/O 多的地方 |

**`async` / `await` 不会创建线程**。真正把工作挪到别的线程上的是 `Task.Run`（第 12 章）。

## 2. 基本写法 🟢

### 2.1 async 方法长什么样

```csharp
async Task DoAsync()                // 没有返回值
{
    await Task.Delay(1000);
}

async Task<int> GetNumberAsync()     // 有返回值：方法里 return int，编译器包装成 Task<int>
{
    await Task.Delay(500);
    return 42;
}

int n = await GetNumberAsync();      // await 把 Task<int> "拆"成 int
```

- `async` 修饰的方法里才能用 `await`。
- 方法名按惯例以 `Async` 结尾。
- `return 42` 返回的是 `int`，但方法签名写 `Task<int>`：**编译器把返回值包装进 Task**，调用方拿到的是 Task。
- `Main` 也可以是 `static async Task Main(string[] args)`。

### 2.2 第一个 await 之前是同步执行的 ⚠️

```csharp
Console.WriteLine($"Main: 线程 {Environment.CurrentManagedThreadId}");
Task<int> task = GetNumberAsync();
Console.WriteLine("Main 继续（GetNumberAsync 遇到 await 之后才返回到这里）");
int number = await task;
Console.WriteLine($"number = {number}");

static async Task<int> GetNumberAsync()
{
    Console.WriteLine($"await 前: 线程 {Environment.CurrentManagedThreadId}（和 Main 相同）");
    await Task.Delay(500);            // 到这里才把控制权交还给调用方
    Console.WriteLine($"await 后: 线程 {Environment.CurrentManagedThreadId}（可能换了线程）");
    return 42;
}
```
```
Main: 线程 1
await 前: 线程 1（和 Main 相同）
Main 继续（GetNumberAsync 遇到 await 之后才返回到这里）
await 后: 线程 5（可能换了线程）
number = 42
```

执行过程：

1. 调用 `GetNumberAsync()`，**在调用方线程上同步执行**，直到遇到第一个"还没完成"的 `await`。
2. 这时方法返回一个未完成的 Task 给调用方，调用方继续往下走。
3. `Task.Delay` 完成后，从线程池取一个线程（这里是 5）执行 `await` 后面的代码。

所以如果 `await` 前面有耗时的同步代码，调用方会被卡住：

```csharp
static async Task WorkAsync()
{
    Console.WriteLine($"  await 前: 线程 {Environment.CurrentManagedThreadId}");
    Thread.Sleep(1000);                // 同步的耗时代码
    Console.WriteLine("  同步部分执行完");
    await Task.Delay(100);
    Console.WriteLine($"  await 后: 线程 {Environment.CurrentManagedThreadId}");
}
```
```
Main: 线程 1
  await 前: 线程 1
  同步部分执行完
WorkAsync 返回了，Status = WaitingForActivation
  await 后: 线程 5
await 之后 Main: 线程 5
```

"WorkAsync 返回了"在 1 秒的同步部分执行完之后才打印。**标了 `async` 不等于方法会在后台跑**。

### 2.3 返回类型

| 返回类型 | 用途 |
|---|---|
| `Task` | 没有返回值（相当于 `void`） |
| `Task<T>` | 返回 `T` |
| `ValueTask<T>` | 经常同步完成（比如命中缓存）的热点方法，可以少分配一个 Task 对象 |
| `void` | ⚠️ **只用于事件处理程序** |
| `IAsyncEnumerable<T>` | 异步地一条一条产出数据（第 5 节） |

`ValueTask` 示例：

```csharp
async ValueTask<string> GetAsync(int id)
{
    if (cache.TryGetValue(id, out var v)) return $"缓存命中: {v}";   // 同步完成，不分配 Task
    await Task.Delay(100);
    cache[id] = $"user{id}";
    return $"查询: {cache[id]}";
}
Console.WriteLine(await GetAsync(1));
Console.WriteLine(await GetAsync(1));
```
```
查询: user1
缓存命中: user1
```

⚠️ `ValueTask` 只能 `await` 一次，不能存起来反复 await。不确定时就用 `Task`。

### 2.4 async void 的危险 ⚠️

`async void` 方法没有 Task 来保存异常，异常直接抛到线程池上，**进程崩溃**，调用方的 try/catch 也接不住：

```csharp
try
{
    BadAsyncVoid();
}
catch (Exception ex)
{
    Console.WriteLine($"捕获到: {ex.Message}");   // 不会执行
}
await Task.Delay(1500);
Console.WriteLine("不会打印");

static async void BadAsyncVoid()
{
    await Task.Delay(500);
    throw new Exception("async void 异常");
}
```
```
Unhandled exception. System.Exception: async void 异常
   at Program.<<Main>$>g__BadAsyncVoid|0_0() in .../c-async-void.cs:line 15
   at System.Threading.Tasks.Task.<>c.<ThrowAsync>b__135_1(Object state)
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   ...
```

进程退出码 134。另外调用方也没法知道它什么时候执行完。

**规则**：除了事件处理程序（`button.Click += async (s, e) => ...`，签名被事件定死了），一律写 `async Task`。

## 3. await vs .Wait() / .Result 🟢

```csharp
try { await FailAsync(); }
catch (InvalidOperationException ex)          // await 抛出原始异常
{
    Console.WriteLine($"await 捕获: {ex.GetType().Name}: {ex.Message}");
}

try { FailAsync().Wait(); }                    // 阻塞等待
catch (AggregateException ex)                  // Wait() / .Result 抛包装后的 AggregateException
{
    Console.WriteLine($"Wait() 捕获: {ex.GetType().Name} → {ex.InnerException?.Message}");
}
```
```
await 捕获: InvalidOperationException: boom
Wait() 捕获: AggregateException → boom
```

| | `await task` | `task.Wait()` / `task.Result` |
|---|---|---|
| 当前线程 | 释放，去干别的 | **阻塞**，干等 |
| 异常形式 | 原始异常（`InvalidOperationException`） | `AggregateException`，原始异常在 `InnerException(s)` 里 |
| 能用的地方 | `async` 方法里 | 任何地方 |
| 风险 | — | 线程池饥饿；UI 程序里可能**死锁** |

**死锁是怎么来的**：WinForms / WPF 这类有 UI 线程的程序里，在 UI 线程上 `.Result`，被等的 async 方法里 `await` 完成后要回到 UI 线程继续执行。UI 线程在等任务，任务在等 UI 线程，互相卡死。这种"同步代码里等异步方法"叫 **sync over async**，原则是**一路 async 到底**。控制台程序和 ASP.NET Core 没有 UI 线程，不会这样死锁，但阻塞线程的浪费还在。

## 4. 不要用 Task.Run 包真正的异步 I/O 🟢

```csharp
// ❌ 多此一举
await Task.Run(async () => await httpClient.GetStringAsync(url));
// ✅ 直接 await
await httpClient.GetStringAsync(url);
```

`GetStringAsync` 本身就是异步的，等待期间不占线程。包一层 `Task.Run` 的结果是：先借一个线程池线程，它只做了"发出请求"这一件小事，遇到 `await` 就还回去，之后的等待和不包完全一样。白白多一次线程切换。

```csharp
await Task.Run(async () =>
{
    Console.WriteLine($"线程 A = {Environment.CurrentManagedThreadId}");
    await Task.Delay(1000);                      // 等待期间线程 A 还回线程池
    Console.WriteLine($"await 之后 = {Environment.CurrentManagedThreadId}");   // 可能是另一个线程
});
```
```
线程 A = 4
await 之后 = 4
```

这次碰巧又拿到了 4 号线程，但中间那 1 秒它是空闲的，可以被别的任务使用。

**也不需要用 Task.Run 来"让多个异步方法同时执行"**，调用异步方法时它就已经开始了：

```csharp
Task<string> a = httpClient.GetStringAsync(url1);   // 已经开始
Task<string> b = httpClient.GetStringAsync(url2);   // 已经开始
await Task.WhenAll(a, b);
```

什么时候包一层有意义：

| 情况 | 要不要用 Task.Run |
|---|---|
| 真正异步的 I/O 方法（HttpClient、文件、数据库） | ❌ 直接 await |
| 想让多个异步方法同时执行 | ❌ 直接调用，再用 WhenAll 等 |
| CPU 密集的同步计算，不想卡住 UI 线程 | ✅ `await Task.Run(() => HeavyCompute())` |
| 异步方法里 await 前有很重的同步部分，或者是"假异步"（内部 `return Task.FromResult(SyncCall())`） | ✅（UI 程序里） |
| 同步代码里迫不得已要调异步方法 | 权宜之计：`Task.Run(() => FooAsync()).Result`，避开 UI 线程死锁 |
| 在类库里把同步方法包装成 `XxxAsync`（async over sync） | ❌ 只提供同步版本，由调用方决定 |
| ASP.NET Core 这类服务端程序 | 基本不需要，只会多一次线程切换 |

一句话：**`Task.Run` 是用来把 CPU 工作挪到别的线程上的，不是用来"让代码变成异步"的。**

## 5. 常用写法 🟢

### 5.1 并发多个请求：WhenAll

```csharp
List<Task<int>> tasks = new List<Task<int>>();
foreach (int n in new[] { 1, 2, 3 })
{
    tasks.Add(Task.Factory.StartNew(DoSomething, n));   // DoSomething 睡 n 秒，返回 n*n
}
int[] results = await Task.WhenAll(tasks);              // 顺序和传入一致，不是完成顺序
Console.WriteLine($"results = {string.Join(", ", results)}");
```
```
main thread start
1 * 1 = 1
2 * 2 = 4
3 * 3 = 9
results = 1, 4, 9
sum = 14, 耗时 3015ms（≈ 最慢的 3 秒，而不是 1+2+3 = 6 秒）
main thread end
```

这个练习里 `DoSomething` 用的是 `Thread.Sleep`，所以要 `StartNew` 放到线程池上。如果是真正的异步方法（`httpClient.GetStringAsync`），直接调用就行。

⚠️ 写成 `foreach (...) { await DoAsync(n); }` 是**一个一个**执行，总耗时是 6 秒。

### 5.2 async 里的取消

把 token 传给 `Task.Delay`、`HttpClient`、`File.ReadAllTextAsync` 等方法，取消**立即生效**（[08-async-cancellation.cs](../advanced/08-async-cancellation.cs)）：

```csharp
// 写法 1：手动检查
static async Task LoopWithCheckAsync(CancellationToken token)
{
    while (true)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");
        await Task.Delay(1000);               // 不传 token：取消后要等这 1 秒结束才发现
        token.ThrowIfCancellationRequested();
    }
}

// 写法 2：token 传给 Task.Delay（推荐）
static async Task LoopWithDelayTokenAsync(CancellationToken token)
{
    while (true)
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");
        await Task.Delay(1000, token);        // 一旦取消，立即结束并抛 OperationCanceledException
    }
}

using var cts = new CancellationTokenSource(3000);   // 3 秒后取消
try { await LoopWithDelayTokenAsync(cts.Token); }
catch (OperationCanceledException ex) { Console.WriteLine(ex.Message); }
```
```
main thread start
17:24:21.877
17:24:22.894
17:24:23.895
写法 1：The operation was canceled.
--------------------------
17:24:24.902
17:24:25.903
17:24:26.904
写法 2：A task was canceled.
main thread end
```

- 写法 2 抛的是 `TaskCanceledException`，它是 `OperationCanceledException` 的子类（实测 `is OperationCanceledException` 为 `True`），所以统一 `catch (OperationCanceledException)` 就行。
- 惯例：async 方法的最后一个参数是 `CancellationToken cancellationToken = default`，一路往下传。

### 5.3 HttpClient 正确用法

[08-httpclient.cs](../advanced/08-httpclient.cs)（代替已过时的 `WebRequest`，用它会有 SYSLIB0014 警告）：

```csharp
// 整个程序共用一个 HttpClient，不要每次请求都 new（否则会耗尽端口）
private static readonly HttpClient httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)       // 默认 100 秒
};

try
{
    // ResponseHeadersRead：收到响应头就返回，正文稍后再读
    using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();     // 不是 2xx 就抛 HttpRequestException
    string content = await response.Content.ReadAsStringAsync();
}
catch (HttpRequestException ex)            // 网络错误、非 2xx 状态码
{
    Console.WriteLine($"请求失败: {ex.StatusCode} {ex.Message}");
}
catch (TaskCanceledException)              // 超时
{
    Console.WriteLine("请求超时");
}
```

这个例子**需要联网**。写笔记时的环境连不上外网（DNS 解析超时），实际输出是：

```
main thread start
请求超时
main thread end
```

说明超时会表现为 `TaskCanceledException`，而不是 `TimeoutException`。联网时会打印状态码、耗时和正文长度。

要点：

| 要点 | 说明 |
|---|---|
| 复用实例 | `HttpClient` 设计成长期复用、线程安全。每次 `using var c = new HttpClient()` 会让连接来不及释放，耗尽端口 |
| ASP.NET Core 里 | 用 `IHttpClientFactory`（`services.AddHttpClient()`），由框架管理连接池，也能处理 DNS 变化 |
| 超时 | `Timeout` 属性（默认 100 秒）；单个请求可以再传 `CancellationToken` |
| 只要字符串 | `await httpClient.GetStringAsync(url)` |
| JSON | `using System.Net.Http.Json;` 然后 `await httpClient.GetFromJsonAsync<T>(url)`，单文件运行需要 `#:property PublishAot=false` |

```csharp
#:property PublishAot=false
using System.Net.Http.Json;
Todo? todo = await http.GetFromJsonAsync<Todo>("https://jsonplaceholder.typicode.com/todos/1");
record Todo(int UserId, int Id, string Title, bool Completed);
```

（同样需要联网，本环境实测输出"请求超时"，未能验证正常返回的内容。）

### 5.4 读写文件

```csharp
string content = await File.ReadAllTextAsync(fileName);   // 最常用，默认 UTF-8

// 需要控制流时：await using 用完自动关闭
await using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read,
                                     FileShare.Read, 4096, useAsync: true))
{
    byte[] buffer = new byte[fs.Length];
    int count = await fs.ReadAsync(buffer);
}
```
```
ReadAllTextAsync:
Hello async
你好，async
--------------------------
await 前：线程 6
await 后：线程 6（可能换了线程）
ReadAsync, length 26:
Hello async
你好，async
```

`useAsync: true` 让 `FileStream` 走操作系统的异步 I/O，等待期间不占线程（Windows 上效果明显）。用 `File.ReadAllTextAsync` 这类方法时不用管，.NET 内部已经设置好了。

### 5.5 老写法：APM（BeginXxx / EndXxx）⚪

.NET 早期的异步模式，在老代码里会见到（[06-apm-begin-read.cs](../advanced/06-apm-begin-read.cs)）：

```csharp
using var done = new ManualResetEvent(false);
var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
byte[] buffer = new byte[fileStream.Length];

fileStream.BeginRead(buffer, 0, buffer.Length, ar =>
{
    try
    {
        int count = fileStream.EndRead(ar);     // 每个 BeginXxx 都要配一个 EndXxx：拿结果、抛异常
        Console.WriteLine($"end reading, length: {count}");
        Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, count));
    }
    finally
    {
        fileStream.Close();
        done.Set();                              // 通知主线程
    }
}, null);
Console.WriteLine("main thread is free to do other work...");
done.WaitOne();                                  // 不等的话 Main 结束，回调来不及执行
```
```
start reading at 17:24:19
end reading, length: 22
Hello APM
你好，APM
main thread is free to do other work...
main end
```

这次回调比"main thread is free..."先打印：文件很小，读操作很快就完成了，两者的先后顺序并不固定。回调、手动关闭、手动发信号，和 Java 的回调地狱一样难写。可以用 `Task.Factory.FromAsync` 包装成 Task（第 12 章），但现在直接用 `ReadAsync` 就行。

⚠️ **委托的 `BeginInvoke` 在 .NET Core 起不再支持**（`Stream.BeginRead` 还保留着）：

```csharp
Func<int, int> job = n => n * n;
job.BeginInvoke(5, null, null);
```
```
PlatformNotSupportedException: Operation is not supported on this platform.
```

老教程里的 `BeginInvoke` / `EndInvoke` / `AsyncCallback` 换成：`Task.Run(() => job(num))` / `await task` / `await` 后面的代码（[05-task-continuation.cs](../advanced/05-task-continuation.cs)）。

### 5.6 ConfigureAwait(false) 🟡

`await` 完成后默认会尝试回到原来的"同步上下文"（`SynchronizationContext`）继续执行。UI 程序里就是回到 UI 线程，这样 `await` 后面才能直接改界面。

```csharp
var ctx = SynchronizationContext.Current;
Console.WriteLine($"控制台程序 SynchronizationContext.Current = {(ctx == null ? "null" : ctx.GetType().Name)}");
Console.WriteLine($"await 前: 线程 {Environment.CurrentManagedThreadId}");
await Task.Delay(100).ConfigureAwait(false);
Console.WriteLine($"ConfigureAwait(false) 之后: 线程 {Environment.CurrentManagedThreadId}");
```
```
控制台程序 SynchronizationContext.Current = null
await 前: 线程 1
ConfigureAwait(false) 之后: 线程 5
```

| 场景 | 要不要写 `ConfigureAwait(false)` |
|---|---|
| 控制台程序、ASP.NET Core | 不需要，本来就没有同步上下文（上面实测是 `null`），写不写一样 |
| WinForms / WPF 的界面代码 | 不要写，否则 `await` 之后不在 UI 线程上，改控件会报错 |
| **给别人用的类库** | 建议每个 `await` 都写：不关心回到哪个线程，也能避免调用方用 `.Result` 时死锁 |

### 5.7 IAsyncEnumerable 与 await foreach 🟡

数据要一条一条异步地拿（分页接口、数据库游标），用 `IAsyncEnumerable<T>`：方法里 `await` 和 `yield return` 混着写，调用方用 `await foreach`：

```csharp
await foreach (int n in GenerateAsync(3))
{
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.f} 收到 {n}");
}

using var cts = new CancellationTokenSource(250);
try
{
    await foreach (int n in GenerateAsync(10).WithCancellation(cts.Token))
        Console.WriteLine($"收到 {n}");
}
catch (OperationCanceledException) { Console.WriteLine("被取消"); }

static async IAsyncEnumerable<int> GenerateAsync(int count,
    [EnumeratorCancellation] CancellationToken token = default)
{
    for (int i = 1; i <= count; i++)
    {
        await Task.Delay(100, token);   // 模拟每条数据要等一会儿
        yield return i;
    }
}
```
```
17:25:21.9 收到 1
17:25:22.0 收到 2
17:25:22.1 收到 3
收到 1
收到 2
被取消
```

每 100ms 来一条，来一条处理一条，不用等全部拿完。EF Core 的 `AsAsyncEnumerable()`、ASP.NET Core 的流式返回都用它。

## 6. 异常：线程、Task、async 各不相同 🟢

### 6.1 Thread 里的未处理异常：进程崩溃

```csharp
AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    Console.WriteLine($"UnhandledException: {(e.ExceptionObject as Exception)?.Message}, IsTerminating = {e.IsTerminating}");
try
{
    new Thread(() => throw new InvalidOperationException("thread boom")).Start();
}
catch (Exception) { Console.WriteLine("捕获不到"); }
Thread.Sleep(500);
Console.WriteLine("main end");
```
```
UnhandledException: thread boom, IsTerminating = True
Unhandled exception. System.InvalidOperationException: thread boom
   at Program.<>c.<<Main>$>b__0_1() in .../h-thread-crash.cs:line 5
   at System.Threading.Thread.StartCallback(Thread* pThread)
```

- 外面的 try/catch 捕获不到：异常发生在另一个线程上。
- "main end" 没有打印，进程退出码 134。
- 只能在线程方法**内部** try/catch（[09-thread-exception.cs](../advanced/09-thread-exception.cs)）。这点和 Java 一样，Java 里线程未捕获异常只是那个线程死掉，**C# 里是整个进程崩溃**，更严重。

### 6.2 Task 里的异常：保存在 Task 里

Task 里的异常不会让进程崩溃，而是保存在 Task 里，等你去"取"（[09-task-exception.cs](../advanced/09-task-exception.cs)）：

```csharp
// 1. 在任务方法内部捕获
await Task.Run(SafeMethod);

// 2a. Wait() / Result：AggregateException
Task task = Task.Run(Method);
try { task.Wait(); }
catch (AggregateException ex)
{
    foreach (Exception item in ex.InnerExceptions) Console.WriteLine($"2a. Wait 捕获: {item.Message}");
}

// 2b. await：原始异常（推荐）
try { await Task.Run(Method); }
catch (Exception ex) { Console.WriteLine($"2b. await 捕获: {ex.Message}"); }

// 3. 在延续任务里处理
await Task.Run(() => throw new Exception("前驱任务异常"))
    .ContinueWith(t => t.Exception?.Handle(e =>
    {
        Console.WriteLine($"3. 延续任务处理: {e.Message}");
        return true;                                 // true：这个异常已处理
    }), TaskContinuationOptions.OnlyOnFaulted);
```
```
1. 方法内部捕获: 子线程异常 xxx
2a. Wait 捕获: 子线程异常 yyy
2b. await 捕获: 子线程异常 yyy
3. 延续任务处理: 前驱任务异常
```

### 6.3 async 方法的异常：try/catch 包住 await

```csharp
try
{
    await StartAsync();               // StartAsync 里 await Task.Delay(1000) 后 throw
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine(ex.StackTrace);
}
```
```
异步方法异常
   at HelloWorld.Program.StartAsync() in .../09-async-method-exception.cs:line 30
   at HelloWorld.Program.Main(String[] args) in .../09-async-method-exception.cs:line 13
```

写法和同步代码一样，堆栈也是清楚的。前提：**要 await**，不 await 就没人去取异常。

### 6.4 AggregateException：InnerExceptions 与 Flatten 🟡

`AggregateException` 是"一组异常"，`Wait()`、`.Result`、`WaitAll`、`task.Exception` 都用它：

| 成员 | 作用 |
|---|---|
| `InnerExceptions` | 所有内部异常（`ReadOnlyCollection<Exception>`） |
| `InnerException` | 第一个 |
| `Flatten()` | 内部异常本身又是 `AggregateException` 时，摊平成一层 |
| `Handle(e => bool)` | 逐个处理，返回 `false` 的会重新抛出 |

嵌套的情况（子任务附加到父任务）：

```csharp
Task outer = Task.Factory.StartNew(() =>
{
    Task.Factory.StartNew(() => throw new InvalidOperationException("child 1"), TaskCreationOptions.AttachedToParent);
    Task.Factory.StartNew(() => throw new ArgumentException("child 2"), TaskCreationOptions.AttachedToParent);
});
try { outer.Wait(); }
catch (AggregateException ex)
{
    Console.WriteLine($"Flatten 前: {string.Join(", ", ex.InnerExceptions.Select(e => e.GetType().Name))}");
    Console.WriteLine($"Flatten 后: {string.Join(", ", ex.Flatten().InnerExceptions.Select(e => $"{e.GetType().Name}({e.Message})").Order())}");
}
```
```
Flatten 前: AggregateException, AggregateException
Flatten 后: ArgumentException(child 2), InvalidOperationException(child 1)
```

### 6.5 await 只抛第一个异常 ⚠️

`WhenAll` 里多个任务都失败时，`await` 只抛出第一个；想拿到全部，看返回的那个 Task 的 `.Exception`：

```csharp
Task all = Task.WhenAll(FailAsync("A", 100), FailAsync("B", 200), OkAsync());
try
{
    await all;
}
catch (Exception ex)
{
    Console.WriteLine($"await 只抛出第一个: {ex.Message}");
    foreach (var e in all.Exception!.InnerExceptions)
        Console.WriteLine($"  all.Exception 里: {e.Message}");
}
```
```
await 只抛出第一个: A
  all.Exception 里: A
  all.Exception 里: B
```

所以要**先把 `WhenAll` 的结果存到变量里**，不要直接 `await Task.WhenAll(...)`，否则 catch 里拿不到它。

### 6.6 全局兜底：UnhandledException vs UnobservedTaskException 🟡

[09-global-exception-handlers.cs](../advanced/09-global-exception-handlers.cs)：

```csharp
AppDomain.CurrentDomain.UnhandledException += (s, e) => { /* 记日志 */ };

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    Console.WriteLine($"UnobservedTaskException: {e.Exception.InnerException?.Message}");
    e.SetObserved();                     // 标记为"已处理"
};

FireAndForget();                         // Task.Run(() => throw ...)，不 await
await Task.Delay(200);
Console.WriteLine("Task 已经失败了，但没人 await 它，所以什么都没发生");

GC.Collect();                            // 手动 GC，平时你不知道 GC 什么时候发生
GC.WaitForPendingFinalizers();
await Task.Delay(100);
Console.WriteLine("GC 之后才触发了 UnobservedTaskException");
```
```
Task 已经失败了，但没人 await 它，所以什么都没发生
UnobservedTaskException: task boom
GC 之后才触发了 UnobservedTaskException
main end
```

| | `AppDomain.UnhandledException` | `TaskScheduler.UnobservedTaskException` |
|---|---|---|
| 什么时候触发 | 任何线程上没被 catch 的异常，**立刻** | 失败的 Task 从来没人看过它的异常，**等它被 GC 回收时** |
| 进程会不会崩溃 | **会**（`IsTerminating = True`），处理程序里只能记日志 | 不会（.NET 4.5 起默认不崩溃） |
| 能做什么 | 记日志、写崩溃报告 | 记日志，`SetObserved()` 标记已处理 |
| Java 对应 | `Thread.setDefaultUncaughtExceptionHandler` | 无直接对应 |

⚠️ 触发时机取决于 GC，**不能**靠 `UnobservedTaskException` 处理业务异常。它只是最后一道日志兜底；正确做法是每个 Task 都 await。

| 异常发生在 | 不处理的后果 | 怎么处理 |
|---|---|---|
| `new Thread` 里 | 进程崩溃 | 线程方法内部 try/catch |
| `Task.Run` 里 | 保存在 Task 里，没人看就"静默丢失" | `await` 外面 try/catch |
| `async Task` 方法里 | 同上 | `await` 外面 try/catch |
| `async void` 方法里 | 进程崩溃 | 方法内部 try/catch；能改就改成 `async Task` |
| `System.Threading.Timer` 回调 | 进程崩溃 | 回调内部 try/catch（第 12 章） |
| `System.Timers.Timer` 的 Elapsed | 被吞掉 | Elapsed 内部 try/catch 记日志（第 12 章） |

## 7. 常见错误：Main 不等任务就退出 ⚠️

```csharp
Console.WriteLine("main start");
Task.Run(() =>
{
    Thread.Sleep(500);
    Console.WriteLine("任务完成");       // 不会打印
});
_ = DoAsync();                           // DoAsync：await Task.Delay(500) 后打印
Console.WriteLine("main end");
```
```
warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
main start
main end
```

线程池线程都是后台线程，Main 一返回进程就结束，任务来不及执行。练习里很多"程序什么都没输出""异常没捕获到"都是这个原因。

- 编译器的 **CS4014** 警告就是在提醒你"这个 Task 没有 await"，不要忽略。
- `_ = DoAsync();` 显式丢弃，能消掉警告，但任务照样不会被等待，只在确实不关心结果时用（fire-and-forget），并且方法内部要自己 try/catch。
- 修正：`static async Task Main`，然后 `await` 每个任务（或 `await Task.WhenAll(...)`）。

## 8. 和 Java 对照

| Java | C# |
|---|---|
| `CompletableFuture<T>` | `Task<T>` |
| `supplyAsync(() -> x)` | `Task.Run(() => x)` |
| `.thenApply(f)` / `.thenCompose(f)` | `await` 后面直接写代码 |
| `.exceptionally(...)` | `try { await ... } catch` |
| `CompletableFuture.allOf(...)` | `Task.WhenAll(...)` |
| `future.join()` 抛 `CompletionException` 包装 | `.Result` 抛 `AggregateException` 包装；`await` 抛原始异常 |
| `Thread.setDefaultUncaughtExceptionHandler` | `AppDomain.CurrentDomain.UnhandledException` |
| JDK 21 虚拟线程：写同步代码，阻塞时 JVM 自动让出载体线程 | `async` / `await`：显式标记等待点，等待时让出线程 |

Java 1.6 时代写异步只能回调或 `Future.get()` 阻塞；`CompletableFuture`（Java 8）要靠一串 `thenXxx` 链。C# 的 `async` / `await` 让异步代码**写起来像同步代码**：顺序、循环、try/catch 都照常写。Java 21 的虚拟线程走的是另一条路：代码不用改，运行时负责"阻塞时让出线程"。两者解决的是同一个问题：**等待 I/O 时不要占着系统线程**。

## 9. 小结

1. `await` 一个 I/O 操作时不占线程；`async` / `await` 本身不创建线程，CPU 工作才用 `Task.Run`。
2. async 方法在第一个 `await` 之前是在调用方线程上**同步执行**的；`return x` 由编译器包装进 `Task<T>`。
3. 返回 `Task` / `Task<T>`；`async void` 的异常会让进程崩溃，只用于事件处理程序。
4. 在 async 代码里用 `await`，不用 `.Wait()` / `.Result`：后者阻塞线程、抛 `AggregateException`，UI 程序里还可能死锁。
5. 不要用 `Task.Run` 包真正的异步 I/O；并发多个请求就是"先全部调用，再 `await Task.WhenAll`"。
6. `HttpClient` 复用一个实例（ASP.NET Core 用 `IHttpClientFactory`），设置 `Timeout`，超时表现为 `TaskCanceledException`；token 一路往下传。
7. 异常：Thread 里未处理 → 进程崩溃；Task 里 → 保存在 Task 里；`await` 只抛第一个，全部异常在 `task.Exception.InnerExceptions`。`UnobservedTaskException` 要等 GC 才触发，只能兜底记日志。
8. Main 不 await 任务就会直接退出；看到 CS4014 警告要处理。

---

上一章：[Task、并行与定时器 🟢](12-task-parallel.md) · [目录](../README.md) · 下一章：[线程同步 🟡](14-synchronization.md)
