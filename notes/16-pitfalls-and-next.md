# 第 16 章 常见坑与下一步 🟢

> 本章练习：无，汇总前面各章的坑。

> 刚从 Java 转到 C#，你很可能已经遇到过这些情况：程序一闪就退出，`Task` 里的输出一行都没打；在 VS Code 里点运行，报一个莫名其妙的 `tempCodeRunnerFile.cs` 编译错误；`Console.ReadKey()` 直接抛异常；JSON 序列化报 "Reflection-based serialization has been disabled"；Debug 下好好的程序 Release 下卡死。
>
> 本章第一部分把这些坑集中列出来，每个坑讲**现象、原因、解决**；第二部分讲学完基础之后往哪走：ASP.NET Core、EF Core，以及它们和 Spring 的对应关系。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。和 VS Code Code Runner 有关的现象没法在命令行完全复现，用"模拟"的方式验证，会单独注明。

## 1. 运行与环境的坑 🟢

### 1.1 Main 不等 Task / Timer / 后台线程就退出，没有输出 ⚠️

**现象**：

```csharp
Console.WriteLine("main start");
Task.Run(() => { Thread.Sleep(100); Console.WriteLine("Task 里的输出"); });
var timer = new System.Threading.Timer(_ => Console.WriteLine("Timer 回调"), null, 100, 1000);
var t = new Thread(() => { Thread.Sleep(100); Console.WriteLine("后台线程输出"); }) { IsBackground = true };
t.Start();
Console.WriteLine("main end");
```
```
warning CS4014: Because this call is not awaited, execution of the current method continues before the call is completed. ...
main start
main end
```

三个"异步"的东西一行都没打出来。

**原因**：线程池线程（`Task.Run`、定时器回调都跑在线程池上）和 `IsBackground = true` 的线程都是**后台线程**。主线程结束时进程就退出，后台线程直接被终止。只有前台线程（`new Thread` 默认就是）会让进程等它。

**解决**：主线程显式等待。

```csharp
Console.WriteLine("main start");
Task task = Task.Run(() => { Thread.Sleep(100); Console.WriteLine("Task 里的输出"); });
using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
var t = new Thread(() => { Thread.Sleep(100); Console.WriteLine("后台线程输出"); }) { IsBackground = true };
t.Start();

await task;                         // 等 Task
await timer.WaitForNextTickAsync(); // 等定时器触发一次
Console.WriteLine("Timer 触发");
t.Join();                           // 等线程
Console.WriteLine("main end");
```
```
main start
Task 里的输出
后台线程输出
Timer 触发
main end
```

（"Task 里的输出"和"后台线程输出"两行，每次运行顺序可能不同。）

注意那条 `CS4014` 警告：它就是在提醒你"这个 Task 没人等"。看到它不要忽略。

### 1.2 Code Runner 只运行了选中的文本 ⚠️

**现象**：在 VS Code 里选中了几行代码（或者鼠标不小心拖选了一段），点 Code Runner 的运行按钮，报编译错误，文件名是 `tempCodeRunnerFile.cs`。

**原因**：Code Runner 默认行为是"有选中文本就只运行选中部分"：它把选中的内容写进同目录下的临时文件 `tempCodeRunnerFile.cs`，再运行这个文件。C# 代码片段离开上下文是编译不过的。

模拟：把一行引用了外部变量的代码单独存成 `tempCodeRunnerFile.cs` 运行：

```
tempCodeRunnerFile.cs(1,37): error CS0103: The name 'account' does not exist in the current context

The build failed. Fix the build errors and run again.
```

**解决**：在 VS Code 的 `settings.json` 里加：

```json
"code-runner.ignoreSelection": true
```

之后不管有没有选中，都运行整个文件。本机的配置里已经有这一项。如果目录里残留了 `tempCodeRunnerFile.cs`，删掉即可。

### 1.3 Code Runner 下 Console.ReadKey 抛异常、ReadLine 挂住 ⚠️

**现象**：程序里有 `Console.ReadKey()`，在 Code Runner 里运行直接抛 `InvalidOperationException`；有 `Console.ReadLine()` 的程序卡住不动，在 OUTPUT 面板里也没法输入。

**原因**：Code Runner 默认把程序输出显示在 OUTPUT 面板里，程序的标准输入是一个**管道**，不是真正的终端：

- `ReadKey` 要读"按键"，必须有真正的控制台，输入被重定向时直接抛异常。
- `ReadLine` 读的是管道，管道一直没有数据、也一直不关闭，所以一直等下去。

命令行下用重定向模拟（`</dev/null` 和管道）：

```csharp
Console.WriteLine($"IsInputRedirected = {Console.IsInputRedirected}");
if (Console.IsInputRedirected)
{
    string? line = Console.ReadLine();
    Console.WriteLine($"ReadLine 返回：{(line is null ? "null" : line)}");
}
Console.WriteLine("按任意键继续...");
var key = Console.ReadKey();
Console.WriteLine($"你按了 {key.KeyChar}");
```

`dotnet run p03-readkey.cs </dev/null`：

```
IsInputRedirected = True
ReadLine 返回：null
按任意键继续...
Unhandled exception. System.InvalidOperationException: Cannot read keys when either application does not have a console or when console input has been redirected. Try Console.Read.
   at System.ConsolePal.ReadKey(Boolean intercept)
```

`echo abc | dotnet run p03-readkey.cs`：`ReadLine` 读到 `abc`，`ReadKey` 同样抛异常。

`sleep 8 | dotnet run p03-readkey.cs`：管道 8 秒内既没有数据也不关闭，`ReadLine` 一直等，整个命令耗时 8 秒，管道关闭后才返回 `null`。Code Runner 的管道不会自己关闭，所以就是"挂住"。

**解决**：

| 办法 | 做法 |
|---|---|
| 让 Code Runner 在终端里运行 | `settings.json` 加 `"code-runner.runInTerminal": true`，程序跑在 VS Code 的集成终端里，可以正常输入 |
| 直接在终端里跑 | `dotnet run xxx.cs` |
| 代码里先判断 | `if (!Console.IsInputRedirected) Console.ReadKey();` |

练习程序最后那句"按任意键退出"其实不需要：`dotnet run` 在终端里运行，程序结束后输出还在。

### 1.4 单文件程序用 JSON / 反射，报 "Reflection-based serialization has been disabled" ⚠️

**现象**：

```csharp
using System.Text.Json;
var p = new Person("Owen", 30);
Console.WriteLine(JsonSerializer.Serialize(p));
record Person(string Name, int Age);
```
```
warning IL2026: Using member 'System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. ...
warning IL3050: Using member 'System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)' which has 'RequiresDynamicCodeAttribute' can break functionality when AOT compiling. ...
Unhandled exception. System.InvalidOperationException: Reflection-based serialization has been disabled for this application. Either use the source generator APIs or explicitly configure the 'JsonSerializerOptions.TypeInfoResolver' property.
```

**原因**：file-based app 默认 `PublishAot=true`（用 `dotnet project convert` 能看到，见[第 15 章](15-engineering.md)）。开了 Native AOT 之后，`System.Text.Json` 默认禁用基于反射的序列化，同时编译器会对反射类 API 报 `IL2026`、`IL3050` 警告。

**解决**：文件第一行加

```csharp
#:property PublishAot=false
```

同样的代码加上这一行后：

```
{"Name":"Owen","Age":30}
```

这就是 `basics/` 练习第一行都写这句的原因。正式项目（`dotnet new console`）默认不开 AOT，没有这个问题。

### 1.5 StringBuilder 找不到：System.Text 不在隐式 using 里

**现象**：

```csharp
var sb = new StringBuilder();
```
```
error CS0246: The type or namespace name 'StringBuilder' could not be found (are you missing a using directive or an assembly reference?)
```

**原因**：`ImplicitUsings` 只自动引入 `System`、`System.Collections.Generic`、`System.IO`、`System.Linq`、`System.Net.Http`、`System.Threading`、`System.Threading.Tasks` 等几个命名空间（完整列表见[第 15 章](15-engineering.md) 3 节）。

**解决**：手动加 `using`。常用的几个：

| 类型 | 需要的 using |
|---|---|
| `StringBuilder`、`Encoding` | `using System.Text;` |
| `JsonSerializer` | `using System.Text.Json;` |
| `Regex` | `using System.Text.RegularExpressions;` |
| `Stopwatch`、`Debug`、`Process` | `using System.Diagnostics;` |
| `Assembly`、`PropertyInfo` | `using System.Reflection;` |
| `ConcurrentDictionary` | `using System.Collections.Concurrent;` |

VS Code 里把光标放在报错的类型上按 `Cmd+.`，能自动补 `using`。

### 1.6 可空引用类型警告一大堆

**现象**：

```csharp
string name = null;
string? input = Console.ReadLine();
Console.WriteLine(input.Length);
Console.WriteLine(name);
class User { public string Email { get; set; } }
```
```
warning CS8618: Non-nullable property 'Email' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
warning CS8600: Converting null literal or possible null value to non-nullable type.
warning CS8602: Dereference of a possibly null reference.
Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
```

（用 `</dev/null` 运行，`ReadLine` 返回 `null`，第 3 行真的抛了 `NullReferenceException`。）

**原因**：项目和 file-based app 默认 `Nullable=enable`。开启后 `string` 表示"不会是 null"，`string?` 才表示"可能是 null"。编译器做静态检查，**只报警告，不改变运行时行为**。Java 里没有这层检查，所以从 Java 过来的人第一次会看到满屏警告。

**解决**：按警告的意思改，而不是关掉。

| 警告 | 含义 | 常见改法 |
|---|---|---|
| `CS8600` | 把可能为 null 的值赋给非空类型 | 变量声明成 `string?` |
| `CS8602` | 对可能为 null 的值调用成员 | 先判断 `if (s is not null)`，或用 `s?.Length`、`s ?? ""` |
| `CS8618` | 非空属性在构造结束时没赋值 | 构造函数里赋值、加 `required`、给默认值 `= "";`，或声明成 `string?` |
| `CS8604` | 把可能为 null 的值传给非空参数 | 同 `CS8602` |

确定不会是 null 但编译器看不出来时，可以用 `!`（null 包容运算符）：`input!.Length`。尽量少用，用错了照样运行时报错。详见[第 9 章](09-modern-syntax.md)。

## 2. 异步与多线程的坑 🟢

### 2.1 async void 的异常让进程崩溃 ⚠️

**现象**：

```csharp
Console.WriteLine("start");
try
{
    Fire();                 // async void：调用方拿不到 Task，也就 catch 不到异常
}
catch (Exception ex)
{
    Console.WriteLine($"catch 到了：{ex.Message}");   // 不会执行
}
await Task.Delay(500);
Console.WriteLine("end");   // 不会执行，进程已经崩溃

async void Fire()
{
    await Task.Delay(100);
    throw new InvalidOperationException("async void 里的异常");
}
```
```
start
Unhandled exception. System.InvalidOperationException: async void 里的异常
   at Program.<<Main>$>g__Fire|0_0() in p07-async-void.cs:line 16
   at System.Threading.Tasks.Task.<>c.<ThrowAsync>b__135_1(Object state)
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   ...
```

进程退出码 134（被 abort）。`try/catch` 完全没用，`end` 也没打印。

**原因**：`async void` 方法没有 `Task` 可以返回，异常没地方存，只能直接抛到线程池线程上，变成未处理异常，进程崩溃。

**解决**：返回 `Task`，调用方 `await`：

```csharp
try
{
    await FireAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"catch 到了：{ex.Message}");
}

async Task FireAsync()
{
    await Task.Delay(100);
    throw new InvalidOperationException("async Task 里的异常");
}
```
```
start
catch 到了：async Task 里的异常
end
```

`async void` 只用在事件处理器上（签名被事件固定死了），而且方法体里要自己 `try/catch`。详见[第 13 章](13-async-await.md)。

### 2.2 .Result / .Wait() 阻塞线程

**现象**：

```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
int n = GetAsync().Result;   // 阻塞当前线程直到完成
Console.WriteLine($"Result = {n}，主线程被阻塞了约 {sw.ElapsedMilliseconds / 100 * 100} ms");

try { FailAsync().Wait(); }
catch (Exception ex) { Console.WriteLine($".Wait() 抛出：{ex.GetType().Name} -> {ex.InnerException?.Message}"); }

try { await FailAsync(); }
catch (Exception ex) { Console.WriteLine($"await 抛出：{ex.GetType().Name} -> {ex.Message}"); }

async Task<int> GetAsync() { await Task.Delay(1000); return 42; }
async Task FailAsync() { await Task.Delay(10); throw new InvalidOperationException("boom"); }
```
```
Result = 42，主线程被阻塞了约 1000 ms
.Wait() 抛出：AggregateException -> boom
await 抛出：InvalidOperationException -> boom
```

**原因和问题**：

| 问题 | 说明 |
|---|---|
| 浪费线程 | 等待期间线程什么都干不了。服务器程序里大量这样写会把线程池耗光 |
| 异常被包一层 | `.Result`、`.Wait()` 抛 `AggregateException`，真正的异常在 `InnerException` 里；`await` 直接抛原始异常 |
| 死锁 | 在有同步上下文的环境（WinForms/WPF 的 UI 线程、老的 ASP.NET）里，`.Result` 等待的任务要回到同一个线程继续执行，而这个线程正被 `.Result` 占着，互相等待就死锁了。控制台程序和 ASP.NET Core 没有这种同步上下文，所以上面的例子不会死锁（本章没有在 UI 程序里实测） |

**解决**：一路 `async/await` 到底（"async all the way"）。控制台程序的 `Main` 可以直接写成 `async Task Main` 或在顶层语句里用 `await`。确实只能同步等待时，用 `GetAwaiter().GetResult()`，至少异常不会被包一层。

### 2.3 Debug 下正常，Release 下死循环 ⚠️

**现象**：一个线程循环检查标志，主线程 500 ms 后把标志改成 `false`：

```csharp
var flag = new Flag();
var t = new Thread(() =>
{
    long count = 0;
    while (flag.Loop) { count++; }
    Console.WriteLine("loop end");
});
t.Start();
Thread.Sleep(500);
flag.Loop = false;
Console.WriteLine("已设置 Loop = false");
t.Join();
Console.WriteLine("main end");

class Flag { public bool Loop = true; }   // 没有 volatile
```

`dotnet run p10-loop.cs`（Debug）：

```
已设置 Loop = false
loop end
main end
```

`dotnet run -c Release p10-loop.cs`（用 `perl -e 'alarm 15; ...'` 限时 15 秒）：

```
已设置 Loop = false
```

`loop end` 永远不出现，15 秒后被强制结束（退出码 142）。

**原因**：Release 下 JIT 开启优化，发现循环体里没有任何地方改 `Loop`，就把读取提到循环外面，相当于 `if (flag.Loop) while (true) { ... }`。另一个线程的修改再也看不到。

**解决**：跨线程共享的标志用 `volatile`、`Volatile.Read/Write`、`Interlocked`，或者干脆用 `CancellationToken`（推荐）。详见[第 14 章](14-synchronization.md)，练习 [11-volatile.cs](../advanced/11-volatile.cs)。**多线程代码一定要在 Release 下再跑一遍。**

### 2.4 .NET Framework 时代的 API 在 .NET Core 不能用 ⚠️

网上很多老教程、老书讲的是 .NET Framework（只能跑在 Windows 上的老版本）。下面这些 API 在现代 .NET 里要么编译不过，要么一调用就抛 `PlatformNotSupportedException`。

```csharp
#pragma warning disable SYSLIB0006, SYSLIB0014, SYSLIB0024
Try("AppDomain.CreateDomain", () => AppDomain.CreateDomain("d2"));
Action a = () => { };
Try("委托 BeginInvoke", () => a.BeginInvoke(null, null));
Try("Thread.Abort", () => Thread.CurrentThread.Abort());
Try("WebRequest.Create", () => Console.WriteLine($"  返回 {System.Net.WebRequest.Create("http://example.com").GetType().Name}，能用但已过时"));

static void Try(string name, Action action)
{
    try { action(); Console.WriteLine($"{name}: OK"); }
    catch (Exception ex) { Console.WriteLine($"{name}: {ex.GetType().Name}"); }
}
```
```
AppDomain.CreateDomain: PlatformNotSupportedException
委托 BeginInvoke: PlatformNotSupportedException
Thread.Abort: PlatformNotSupportedException
  返回 HttpWebRequest，能用但已过时
WebRequest.Create: OK
```

不加 `#pragma` 时，编译器会给出警告，提示已经写得很清楚：

```
warning SYSLIB0024: 'AppDomain.CreateDomain(string)' is obsolete: 'Creating and unloading AppDomains is not supported and throws an exception.'
warning SYSLIB0006: 'Thread.Abort()' is obsolete: 'Thread.Abort is not supported and throws PlatformNotSupportedException.'
warning SYSLIB0014: 'WebRequest.Create(string)' is obsolete: 'WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.'
```

`CallContext` 更彻底，整个命名空间都没有了：

```csharp
System.Runtime.Remoting.Messaging.CallContext.LogicalSetData("user", "Owen");
```
```
error CS0234: The type or namespace name 'Messaging' does not exist in the namespace 'System.Runtime.Remoting' (are you missing an assembly reference?)
```

| 老 API | 现状（实测） | 替代 | 见 |
|---|---|---|---|
| `AppDomain.CreateDomain` | 抛 `PlatformNotSupportedException`，`SYSLIB0024` | 动态加载卸载用 `AssemblyLoadContext`，隔离用独立进程 | [第 10 章](10-reflection-attributes.md) |
| 委托 `BeginInvoke` / `EndInvoke` | 抛 `PlatformNotSupportedException` | `Task.Run` + `await` | [第 12 章](12-task-parallel.md) |
| `CallContext.LogicalSetData` | 编译错误 `CS0234` | `AsyncLocal<T>` | [第 11 章](11-threads.md) |
| `WebRequest` / `HttpWebRequest` / `WebClient` | 能用但 `SYSLIB0014` 过时 | `HttpClient` | [第 13 章](13-async-await.md) |
| `Thread.Abort` | 抛 `PlatformNotSupportedException`，`SYSLIB0006` | `CancellationToken` 协作式取消 | [第 12 章](12-task-parallel.md) |

看到 `SYSLIBxxxx` 警告就说明用了过时 API，警告里的链接（`https://aka.ms/dotnet-warnings/SYSLIB0014` 这种）会告诉你用什么替代。

## 3. 语法与类库的坑 🟢

### 3.1 字符串比较：C# 用 == 就对了

Java 里 `==` 比较的是引用，必须用 `equals`。C# 的 `string` 重载了 `==`，比较的是**内容**：

```csharp
string a = "hello";
string b = new string("hello".ToCharArray());   // 运行时新建的字符串，和 a 不是同一个对象
Console.WriteLine($"a == b              : {a == b}");
Console.WriteLine($"a.Equals(b)         : {a.Equals(b)}");
Console.WriteLine($"ReferenceEquals(a,b): {ReferenceEquals(a, b)}");
object oa = a, ob = b;
Console.WriteLine($"(object)a == (object)b: {oa == ob}   <- 变量类型是 object 时 == 比的是引用");
Console.WriteLine($"忽略大小写: {string.Equals("Hello", "HELLO", StringComparison.OrdinalIgnoreCase)}");
```
```
a == b              : True
a.Equals(b)         : True
ReferenceEquals(a,b): False
(object)a == (object)b: False   <- 变量类型是 object 时 == 比的是引用
忽略大小写: True
```

⚠️ 例外：变量的**编译时类型**是 `object` 时，`==` 用的是 `object` 的版本，比较引用。忽略大小写用 `StringComparison.OrdinalIgnoreCase`，不要 `a.ToLower() == b.ToLower()`。

### 3.2 算钱用 decimal，不用 double

```csharp
double d = 0.1 + 0.2;
decimal m = 0.1m + 0.2m;
Console.WriteLine($"double : 0.1 + 0.2 = {d:R}，== 0.3 ? {d == 0.3}");
Console.WriteLine($"decimal: 0.1 + 0.2 = {m}，== 0.3 ? {m == 0.3m}");
double total = 0; decimal totalM = 0;
for (int i = 0; i < 10; i++) { total += 0.1; totalM += 0.1m; }
Console.WriteLine($"10 个 0.1 相加：double = {total:R}，decimal = {totalM}");
```
```
double : 0.1 + 0.2 = 0.30000000000000004，== 0.3 ? False
decimal: 0.1 + 0.2 = 0.3，== 0.3 ? True
10 个 0.1 相加：double = 0.9999999999999999，decimal = 1.0
```

`double` 是二进制浮点数，0.1 存不精确。`decimal` 是十进制，金额、汇率用它（相当于 Java 的 `BigDecimal`，但用起来像普通数字，直接 `+ - * /`）。字面量要加 `m` 后缀。BankDemo 的 `Transaction.Amount` 就是 `decimal`。

### 3.3 Random.Next 的上界不包含

```csharp
int min = int.MaxValue, max = int.MinValue;
for (int i = 0; i < 1_000_000; i++)
{
    int n = Random.Shared.Next(1, 10);   // 想要 1~10？
    min = Math.Min(min, n); max = Math.Max(max, n);
}
Console.WriteLine($"Next(1, 10) 一百万次：最小 {min}，最大 {max}");
```
```
Next(1, 10) 一百万次：最小 1，最大 9
```

`Next(min, max)` 的范围是 `[min, max)`，和 Java 的 `nextInt(bound)` 一样左闭右开。要 1～10 写 `Next(1, 11)`。另外用 `Random.Shared`，不要在循环里反复 `new Random()`。

### 3.4 foreach 里修改集合

```csharp
var list = new List<int> { 1, 2, 3, 4 };
try
{
    foreach (var x in list)
        if (x % 2 == 0) list.Remove(x);
}
catch (InvalidOperationException ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }

list = new List<int> { 1, 2, 3, 4 };
list.RemoveAll(x => x % 2 == 0);
Console.WriteLine($"RemoveAll 之后：[{string.Join(", ", list)}]");

var dict = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
foreach (var key in dict.Keys.ToList())   // 先 ToList 拷一份再改
    dict[key] *= 10;
Console.WriteLine($"字典：{string.Join(", ", dict)}");
```
```
InvalidOperationException: Collection was modified; enumeration operation may not execute.
RemoveAll 之后：[1, 3]
字典：[a, 10], [b, 20]
```

和 Java 的 `ConcurrentModificationException` 是一回事。解决：`RemoveAll`、LINQ 生成新集合（`list.Where(...).ToList()`）、先 `ToList()` 拷一份再遍历，或者倒序 `for` 循环按下标删。

### 3.5 Dictionary 取不存在的 key 抛异常

Java 的 `map.get(key)` 不存在时返回 `null`；C# 的索引器 `dict[key]` 直接抛异常：

```csharp
var stock = new Dictionary<string, int> { ["apple"] = 5 };
try { Console.WriteLine(stock["banana"]); }
catch (KeyNotFoundException ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }

if (stock.TryGetValue("banana", out int n)) Console.WriteLine(n);
else Console.WriteLine("TryGetValue: 没有 banana");
Console.WriteLine($"GetValueOrDefault: {stock.GetValueOrDefault("banana")}，带默认值：{stock.GetValueOrDefault("banana", -1)}");
stock["banana"] = 3;   // 用索引器赋值：不存在就新增，存在就覆盖
Console.WriteLine($"赋值后：{stock["banana"]}");
try { stock.Add("apple", 1); }
catch (ArgumentException ex) { Console.WriteLine($"Add 重复 key：{ex.GetType().Name}: {ex.Message}"); }
```
```
KeyNotFoundException: The given key 'banana' was not present in the dictionary.
TryGetValue: 没有 banana
GetValueOrDefault: 0，带默认值：-1
赋值后：3
Add 重复 key：ArgumentException: An item with the same key has already been added. Key: apple
```

| 操作 | key 不存在 | key 已存在 | Java 对应 |
|---|---|---|---|
| `dict[key]`（读） | 抛 `KeyNotFoundException` | 返回值 | `get`（返回 null） |
| `TryGetValue(key, out v)` | 返回 `false` | 返回 `true` | `containsKey` + `get` |
| `GetValueOrDefault(key, def)` | 返回默认值 | 返回值 | `getOrDefault` |
| `dict[key] = v`（写） | 新增 | 覆盖 | `put` |
| `Add(key, v)` | 新增 | 抛 `ArgumentException` | — |
| `TryAdd(key, v)` | 新增，返回 `true` | 不动，返回 `false` | `putIfAbsent` |

### 3.6 坑速查表

| 现象 | 原因 | 解决 | 见 |
|---|---|---|---|
| 程序没输出就退出 | 主线程不等后台线程 | `await` / `Join` | 1.1 |
| `tempCodeRunnerFile.cs` 编译错误 | Code Runner 只运行选中文本 | `code-runner.ignoreSelection` | 1.2 |
| `ReadKey` 抛 `InvalidOperationException` | 输入被重定向 | `code-runner.runInTerminal`、`IsInputRedirected` | 1.3 |
| `ReadLine` 卡住 | 输入是不关闭的管道 | `code-runner.runInTerminal` | 1.3 |
| JSON 报反射序列化被禁用 | 单文件默认 `PublishAot=true` | `#:property PublishAot=false` | 1.4 |
| `CS0246` 找不到 `StringBuilder` | 不在隐式 using 里 | `using System.Text;` | 1.5 |
| 满屏 `CS86xx` | 可空引用类型检查 | 按警告改代码 | 1.6 |
| `try/catch` 抓不到异常，进程崩溃 | `async void` | 改成 `async Task` 并 `await` | 2.1 |
| 抛 `AggregateException`、线程被卡 | `.Result` / `.Wait()` | `await` | 2.2 |
| Release 下死循环 | JIT 优化 + 没有 `volatile` | `volatile` / `CancellationToken` | 2.3 |
| `PlatformNotSupportedException`、`SYSLIBxxxx` | .NET Framework 的老 API | 见替代表 | 2.4 |
| 0.1 + 0.2 != 0.3 | `double` 精度 | `decimal` | 3.2 |
| 随机数取不到上界 | `Next` 左闭右开 | `Next(1, 11)` | 3.3 |
| `Collection was modified` | `foreach` 里改集合 | `RemoveAll` / `ToList()` | 3.4 |
| `KeyNotFoundException` | 索引器读不存在的 key | `TryGetValue` / `GetValueOrDefault` | 3.5 |

## 4. 下一步学什么

C# 语言本身学到这里已经够写业务了。后端开发接下来是 **ASP.NET Core**（相当于 Spring Boot + Spring MVC）和 **EF Core**（相当于 JPA/Hibernate）。

### 4.1 ASP.NET Core 🟢

建议按这个顺序学：

| 顺序 | 内容 | 要掌握什么 | Spring 对应 |
|---|---|---|---|
| 1 | Minimal API | `dotnet new web`；`app.MapGet("/accounts/{id}", ...)`；路由参数、返回 JSON、`Results.NotFound()` | 最简单的 `@RestController` |
| 2 | Controller | `dotnet new webapi --use-controllers`；`[ApiController]`、`[Route]`、`[HttpGet]`；模型绑定和验证 | `@RestController`、`@GetMapping`、`@Valid` |
| 3 | 中间件 | 请求管道、`app.Use(...)`、顺序很重要；异常处理、日志、CORS 都是中间件 | Servlet `Filter`、`HandlerInterceptor` |
| 4 | 依赖注入 | `builder.Services.AddScoped<IAccountService, AccountService>()`；构造函数注入；三种生命周期 | `@Component` / `@Bean` + 构造器注入 |
| 5 | 配置 | `appsettings.json`、`appsettings.Development.json`、环境变量；`IOptions<T>` 绑定强类型配置 | `application.yml`、`@ConfigurationProperties` |
| 6 | 日志 | `ILogger<T>` 注入；日志级别在配置文件里改 | SLF4J + Logback |
| 7 | 认证授权 | JWT Bearer、`[Authorize]`、策略授权 | Spring Security |

三种 DI 生命周期，和 Spring scope 对照：

| ASP.NET Core | 含义 | Spring |
|---|---|---|
| `AddSingleton` | 整个应用一个实例 | `singleton`（默认） |
| `AddScoped` | 每个 HTTP 请求一个实例 | `request` |
| `AddTransient` | 每次注入都新建 | `prototype` |

⚠️ 不要在 Singleton 里注入 Scoped 服务（比如在单例里用 `DbContext`）。

### 4.2 EF Core 🟢

| 主题 | 要掌握什么 | JPA 对应 |
|---|---|---|
| `DbContext` | 一个类继承 `DbContext`，每张表一个 `DbSet<T>` 属性；在 DI 里用 `AddDbContext` 注册为 Scoped | `EntityManager` |
| 实体与映射 | 普通类 + 约定（`Id` 自动当主键）；复杂的用 Fluent API 或特性 | `@Entity`、`@Id` |
| 迁移 | `dotnet ef migrations add Init`、`dotnet ef database update`（先装工具 `dotnet tool install --global dotnet-ef`） | Flyway / Liquibase |
| LINQ 查询 | `db.Accounts.Where(a => a.Balance > 100).ToListAsync()`，LINQ 被翻译成 SQL | JPQL / Criteria API |
| 变更跟踪 | 查出来的实体被跟踪，改属性后 `SaveChangesAsync()` 自动生成 `update` | 持久化上下文 + flush |
| `AsNoTracking()` | 只读查询不跟踪，省内存、更快 | 只读事务 / `@Transactional(readOnly = true)` |
| N+1 问题 | 循环里访问导航属性导致每次一条 SQL；用 `Include(a => a.Transactions)` 一次查出来，或用 `Select` 投影 | `JOIN FETCH`、`@EntityGraph` |

学 EF Core 时打开 SQL 日志，每写一个查询都看一眼生成的 SQL，N+1 问题和查了多余的列都能直接看出来。

### 4.3 Microsoft.Extensions.* 和 Spring 对照

ASP.NET Core 的基础设施都在 `Microsoft.Extensions.*` 这一组包里，控制台程序也能用（通过 `Host.CreateApplicationBuilder`）。

| 功能 | .NET | Spring |
|---|---|---|
| 应用宿主 | `Microsoft.Extensions.Hosting`（`Host`、`WebApplication`） | Spring Boot `SpringApplication` |
| 依赖注入 | `Microsoft.Extensions.DependencyInjection`（`IServiceCollection`） | Spring IoC 容器 |
| 配置 | `Microsoft.Extensions.Configuration`（`IConfiguration`） | `Environment`、`@Value` |
| 强类型配置 | `Microsoft.Extensions.Options`（`IOptions<T>`） | `@ConfigurationProperties` |
| 日志 | `Microsoft.Extensions.Logging`（`ILogger<T>`） | SLF4J |
| 后台任务 | `BackgroundService` / `IHostedService` | `@Scheduled`、`ApplicationRunner` |
| HTTP 客户端 | `IHttpClientFactory` | `RestTemplate` / `WebClient` |
| 缓存 | `IMemoryCache`、`IDistributedCache` | Spring Cache |
| 健康检查 | `AddHealthChecks()` | Spring Boot Actuator |
| ORM | EF Core | JPA / Hibernate |
| 单元测试 | xUnit + Moq / NSubstitute | JUnit + Mockito |

### 4.4 推荐练习路线

1. **把 BankDemo 改成 Web API**：新建 `BankDemo.Api`（`dotnet new webapi`），引用 `BankDemo.Core`；提供 `POST /accounts`、`POST /accounts/{id}/deposit`、`GET /accounts/{id}`；`ITransactionParser` 通过 DI 注入。
2. **加数据库**：用 EF Core + SQLite（不用装数据库服务）保存账户和交易；写迁移；查询交易列表时试试有没有 N+1。
3. **加测试**：Core 继续用单元测试；API 用 `Microsoft.AspNetCore.Mvc.Testing` 的 `WebApplicationFactory` 写集成测试。
4. **加配置和日志**：透支额度放到 `appsettings.json`，用 `IOptions<T>` 读；关键操作用 `ILogger` 记录。
5. **加认证**：JWT，只有本人能查自己的账户。
6. 回头把前面各章的坑在 Web 项目里再检查一遍：有没有 `async void`、`.Result`、`double` 算钱。

### 4.5 官方文档入口

| 内容 | 链接 |
|---|---|
| .NET 总入口 | https://learn.microsoft.com/dotnet/ |
| C# 语言 | https://learn.microsoft.com/dotnet/csharp/ |
| ASP.NET Core | https://learn.microsoft.com/aspnet/core/ |
| EF Core | https://learn.microsoft.com/ef/core/ |
| 代码分析规则（CA） | https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ |
| NuGet 包搜索 | https://www.nuget.org/ |

## 5. 小结

1. 主线程结束进程就退出，后台线程（含 `Task.Run`、定时器）会被直接杀掉；要 `await`、`Join`，并重视 `CS4014` 警告。
2. VS Code Code Runner 配 `ignoreSelection` 和 `runInTerminal`，避免 `tempCodeRunnerFile.cs` 和输入被重定向的问题。
3. 单文件程序默认 `PublishAot=true`，用反射或 JSON 时加 `#:property PublishAot=false`。
4. `async void` 只给事件处理器用；不要 `.Result` / `.Wait()`，一路 `await` 到底。
5. 多线程代码在 Release 下再跑一遍；共享标志用 `volatile` 或 `CancellationToken`。
6. 老教程里的 `AppDomain.CreateDomain`、`BeginInvoke`、`CallContext`、`WebRequest`、`Thread.Abort` 都有现代替代，看到 `SYSLIBxxxx` 就要换。
7. 和 Java 的差异：字符串用 `==`；金额用 `decimal`；`Dictionary` 索引器读不存在的 key 会抛异常。
8. 下一步：ASP.NET Core（Minimal API → Controller → 中间件 → DI/配置/日志 → 认证授权）+ EF Core，从把 BankDemo 改成 Web API 开始。

---

上一章：[项目、测试与 NuGet 🟢](15-engineering.md) · [目录](../README.md)
