# 第 8 章 异常与资源释放 🟢

> 本章练习：[19-custom-exception.cs](../basics/19-custom-exception.cs)

> 写代码时你会遇到这些问题：
> 方法里读文件，编译器居然不要求写 `throws` 也不要求 `try/catch`；日志里的异常堆栈只到 `catch` 那一行，真正出错的位置找不到了；`int.Parse` 遇到用户乱输就抛异常，到处 `try/catch` 又慢又难看；文件、数据库连接用完忘了关；Java 的 try-with-resources 在 C# 里怎么写。
> 本章讲异常怎么抛、怎么捕获、怎么重新抛才不丢信息，以及 `IDisposable` + `using` 释放资源。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. try / catch / finally

基本语法和 Java 一样。练习 19：

```csharp
var person = new Person(-18);
try
{
    Check(person);
}
catch (DataEntityException<Person> ex)      // 只捕获这一种异常，其他异常继续往外抛
{
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"Entity.Age: {ex.Entity.Age}");   // 异常里还能带上出问题的对象
}
finally
{
    Console.WriteLine("finally：无论是否异常都会执行");
}
```
```
Message: age must be greater than 0
Entity.Age: -18
finally：无论是否异常都会执行
```

和 Java 的小区别：

| | C# | Java |
|---|---|---|
| 只要类型不要变量 | `catch (FormatException)` | 必须写变量名 |
| 捕获所有 | `catch (Exception ex)` 或 `catch { }` | `catch (Exception e)` |
| 一个 catch 捕获多种 | 用 `when` 过滤器（见第 4 节） | `catch (A \| B e)` |
| 异常信息 | `ex.Message`（属性） | `e.getMessage()` |
| 堆栈 | `ex.StackTrace`、`ex.ToString()` | `e.printStackTrace()` |
| 原因 | `ex.InnerException` | `e.getCause()` |

多个 `catch` 时，**子类写在前面，父类写在后面**（写反了编译报错 CS0160），和 Java 一样。

## 2. C# 没有受检异常 ⚠️

Java 里 `IOException` 是受检异常（checked exception），方法要么 `throws` 声明，要么 `try/catch`，否则编译不过。**C# 没有这个机制，所有异常都相当于 Java 的 `RuntimeException`**：

```csharp
Console.WriteLine("开始");
ReadConfig("/no/such/config.json");   // 没有 throws 声明，也不强制 try/catch
Console.WriteLine("不会执行到这里");

static string ReadConfig(string path) => File.ReadAllText(path);
```
```
开始
Unhandled exception. System.IO.DirectoryNotFoundException: Could not find a part of the path '/no/such/config.json'.
Directory path: '/no/such/config.json'
   at Interop.ThrowExceptionForIoErrno(ErrorInfo errorInfo, String path, Boolean isDirError)
   ...（中间省略框架内部的几行）
   at System.IO.File.ReadAllText(String path, Encoding encoding)
   at Program.<<Main>$>g__ReadConfig|0_0(String path) in .../unhandled.cs:line 5
   at Program.<Main>$(String[] args) in .../unhandled.cs:line 2
```

编译通过，运行时异常没人捕获，进程直接退出（退出码 134）。

这意味着：

- 编译器**不会提醒**你哪些方法可能抛异常，要靠文档（方法注释里的 `<exception>`）和经验。
- 不需要在每一层都写 `try/catch` 或包装成 `RuntimeException`，**在能处理的地方捕获**就行：比如 Web 接口的最外层统一捕获、返回 500。
- 不要为了"保险"到处 `catch (Exception) { }` 吞掉异常，那样出了问题完全没有线索。

## 3. 常见异常类型

实测各种常见错误抛出的异常：

```
s.Length               -> NullReferenceException: Object reference not set to an instance of an object.
arr[5]                 -> IndexOutOfRangeException: Index was outside the bounds of the array.
(int)o                 -> InvalidCastException: Unable to cast object of type 'System.String' to type 'System.Int32'.
int.Parse("abc")       -> FormatException: The input string 'abc' was not in a correct format.
10 / zero              -> DivideByZeroException: Attempted to divide by zero.
checked overflow       -> OverflowException: Arithmetic operation resulted in an overflow.
dict["x"]              -> KeyNotFoundException: The given key 'x' was not present in the dictionary.
new List<int>().First() -> InvalidOperationException: Sequence contains no elements
NotImplemented         -> NotImplementedException: The method or operation is not implemented.
File.ReadAllText       -> DirectoryNotFoundException: Could not find a part of the path '/no/such/file.txt'.
```

| C# | Java 对应 | 什么时候出现 / 什么时候自己抛 |
|---|---|---|
| `NullReferenceException` | `NullPointerException` | 对 `null` 调用成员。**不要自己抛** |
| `ArgumentNullException` | `NullPointerException`（`Objects.requireNonNull`） | 参数不该为 `null` 却传了 `null` |
| `ArgumentException` | `IllegalArgumentException` | 参数值不合法 |
| `ArgumentOutOfRangeException` | `IllegalArgumentException` / `IndexOutOfBoundsException` | 参数超出范围 |
| `InvalidOperationException` | `IllegalStateException` | 对象当前状态不允许这个操作 |
| `NotSupportedException` | `UnsupportedOperationException` | 不支持的操作（如只读集合 `Add`） |
| `NotImplementedException` | 无（常用 `UnsupportedOperationException`） | 还没写完的方法，不该上线 |
| `IndexOutOfRangeException` | `ArrayIndexOutOfBoundsException` | 数组越界 |
| `InvalidCastException` | `ClassCastException` | 强制类型转换失败 |
| `FormatException` | `NumberFormatException` | 字符串格式不对 |
| `KeyNotFoundException` | 无（`HashMap.get` 返回 `null`） | 字典取不存在的 key |
| `IOException` 及子类 | `IOException` | 文件、网络读写失败 |
| `ObjectDisposedException` | 无 | 对象已经 `Dispose` 了还在用 |
| `OperationCanceledException` | 无 | 异步操作被取消（见[第 13 章](13-async-await.md)） |

自己写代码校验参数时，按上表选 `ArgumentNullException` / `ArgumentException` / `ArgumentOutOfRangeException` / `InvalidOperationException`，不要直接 `throw new Exception("...")`。

## 4. 异常过滤器 when

`catch` 后面可以加条件，条件为 `true` 才进这个 `catch`：

```csharp
foreach (var code in new[] { 404, 500, 200 })
{
    try
    {
        Call(code);
    }
    catch (HttpError ex) when (ex.Code == 404)
    {
        Console.WriteLine("404：资源不存在，返回空");
    }
    catch (HttpError ex) when (ex.Code >= 500)
    {
        Console.WriteLine($"{ex.Code}：服务端错误，稍后重试");
    }
}

static void Call(int code) { if (code != 200) throw new HttpError(code); Console.WriteLine("200 OK"); }
class HttpError(int code) : Exception($"HTTP {code}") { public int Code { get; } = code; }
```
```
404：资源不存在，返回空
500：服务端错误，稍后重试
200 OK
```

Java 里要先 `catch` 再在里面 `if`，不满足再 `throw`。`when` 不满足时异常**根本没被捕获**，继续往外传，堆栈完整。

一个常见技巧：在 `when` 里记日志并返回 `false`，只记录、不捕获：

```csharp
try
{
    try { Call(503); }
    catch (Exception ex) when (Log(ex)) { Console.WriteLine("不会到这里"); }
}
catch (HttpError ex) { Console.WriteLine($"外层捕获 {ex.Code}"); }

static bool Log(Exception ex) { Console.WriteLine($"日志：{ex.Message}"); return false; }
```
```
日志：HTTP 503
外层捕获 503
```

一个 `catch` 处理多种异常（相当于 Java 的 `catch (A | B e)`）：

```csharp
catch (Exception ex) when (ex is FormatException or OverflowException)
```

## 5. throw vs throw ex ⚠️

捕获之后要重新抛出时，**写 `throw;`，不要写 `throw ex;`**。实测对比：

```csharp
Console.WriteLine("===== throw; =====");
try { Rethrow(); } catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
Console.WriteLine("===== throw ex; =====");
try { RethrowEx(); } catch (Exception ex) { Console.WriteLine(ex.StackTrace); }

static void Inner() => throw new InvalidOperationException("库存不足");   // 第 8 行
static void Rethrow()
{
    try { Inner(); }                                                     // 第 11 行
    catch (Exception) { Console.WriteLine("记录日志..."); throw; }
}
static void RethrowEx()
{
    try { Inner(); }
    catch (Exception ex) { Console.WriteLine("记录日志..."); throw ex; }  // 第 17 行
}
```
```
===== throw; =====
记录日志...
   at Program.<<Main>$>g__Inner|0_0() in .../rethrow.cs:line 8
   at Program.<<Main>$>g__Rethrow|0_1() in .../rethrow.cs:line 11
   at Program.<Main>$(String[] args) in .../rethrow.cs:line 2
===== throw ex; =====
记录日志...
   at Program.<<Main>$>g__RethrowEx|0_2() in .../rethrow.cs:line 17
   at Program.<Main>$(String[] args) in .../rethrow.cs:line 4
```

- `throw;` 保留了原始堆栈，能看到异常真正出自 `Inner`（第 8 行）。
- `throw ex;` 把堆栈**从这一行重新开始**，`Inner` 那一帧丢了，只知道是第 17 行抛的。
- 编译器会对 `throw ex;` 给出警告：`warning CA2200: Re-throwing caught exception changes stack information`。

⚠️ 这和 Java 不一样：Java 里 `throw e;` 不会重置堆栈（堆栈在 `new` 异常时就确定了）。从 Java 过来的人最容易在这里踩坑。

### 5.1 包装成新异常

想换一个更有业务含义的异常时，把原异常作为 `innerException` 传进去：

```csharp
static void Wrap()
{
    try { Inner(); }
    catch (Exception ex) { throw new ApplicationException("下单失败", ex); }
}
```
```
ApplicationException: 下单失败
  Inner: InvalidOperationException: 库存不足
```

相当于 Java 的 `new RuntimeException("下单失败", e)`。`ex.ToString()` 会把内层异常的堆栈也打印出来。

## 6. 自定义异常

练习 19 的写法：

```csharp
// 自定义异常：继承 Exception，类名按惯例以 Exception 结尾
class DataEntityException<T> : Exception
{
    public T Entity { get; }

    // : base(message) 把消息交给父类 Exception，这样 ex.Message 才能拿到它
    // （相当于 Java 的 super(message)）
    public DataEntityException(T entity, string message) : base(message)
    {
        Entity = entity;
    }
}
```

要点：

- 继承 `Exception`（Java 继承 `RuntimeException`，C# 没有这个区分）。
- 名字以 `Exception` 结尾。
- 构造函数用 `: base(message)` 把消息传给父类；需要包装时再加一个 `(string message, Exception inner) : base(message, inner)`。
- 可以带额外属性（`Entity`、`Code`），调用方在 `catch` 里拿来用。
- 不要继承 `ApplicationException`，那是早期的设计，官方已经不推荐。

什么时候值得自定义：调用方需要**按类型区分处理**（比如 `InsufficientBalanceException` 要提示用户，其他异常要报警）。只是参数不对时，用现成的 `ArgumentException` 就够了。

## 7. 不要用异常做流程控制：TryParse 模式

用户输入、外部数据"格式不对"是**预料之中**的情况，不该靠抛异常来判断：

```csharp
string[] inputs = ["12", "abc", "", "99999999999", " 7 "];
foreach (var input in inputs)
{
    if (int.TryParse(input, out int n)) Console.WriteLine($"\"{input}\" -> {n}");
    else Console.WriteLine($"\"{input}\" -> 不是合法 int");
}
```
```
"12" -> 12
"abc" -> 不是合法 int
"" -> 不是合法 int
"99999999999" -> 不是合法 int
" 7 " -> 7
```

性能对比（输入都是 `"abc"`，各 10 万次）：

```csharp
for (int i = 0; i < 100_000; i++) { try { int.Parse(bad); } catch (FormatException) { } }
for (int i = 0; i < 100_000; i++) { int.TryParse(bad, out _); }
```
```
Parse + catch 10 万次: 189 ms
TryParse     10 万次: 3 ms
```

抛异常要收集堆栈，比一次普通判断贵得多（这次实测差 60 倍左右，具体数字每台机器不同）。

`TryXxx` 模式在 .NET 里到处都是：返回 `bool` 表示成功与否，结果放在 `out` 参数里。

| 会抛异常 | 不抛异常 |
|---|---|
| `int.Parse` / `DateTime.Parse` / `Guid.Parse` | `int.TryParse` / `DateTime.TryParse` / `Guid.TryParse` |
| `dict[key]` | `dict.TryGetValue(key, out var v)` |
| `dict.Add` | `dict.TryAdd` |
| `queue.Dequeue()` | `queue.TryDequeue(out var x)` |
| `list.First()` | `list.FirstOrDefault()` |

原则：**能预料、能正常处理的情况用返回值；真正的意外才用异常。**

## 8. 参数校验的新 API

以前要写：

```csharp
if (name == null) throw new ArgumentNullException(nameof(name));
```

.NET 6 之后有一组静态方法，一行搞定，而且会**自动带上参数名**：

```
ThrowIfNull            -> ArgumentNullException: Value cannot be null. (Parameter 'name')
ThrowIfNullOrEmpty     -> ArgumentException: The value cannot be an empty string. (Parameter '""')
ThrowIfNullOrWhiteSpace -> ArgumentException: The value cannot be an empty string or composed entirely of whitespace. (Parameter '"  "')
ThrowIfNegative        -> ArgumentOutOfRangeException: amount ('-5') must be a non-negative value. (Parameter 'amount')
Actual value was -5.
ThrowIfGreaterThan     -> ArgumentOutOfRangeException: age ('200') must be less than or equal to '150'. (Parameter 'age')
Actual value was 200.
ThrowIfZero            -> ArgumentOutOfRangeException: n ('0') must be a non-zero value. (Parameter 'n')
Actual value was 0.
```

| 方法 | 用途 |
|---|---|
| `ArgumentNullException.ThrowIfNull(x)` | `x` 为 `null` 抛 |
| `ArgumentException.ThrowIfNullOrEmpty(s)` | 字符串为 `null` 或 `""` 抛 |
| `ArgumentException.ThrowIfNullOrWhiteSpace(s)` | 再加上全是空白也抛 |
| `ArgumentOutOfRangeException.ThrowIfNegative(n)` | `n < 0` 抛 |
| `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(n)` | `n <= 0` 抛 |
| `ArgumentOutOfRangeException.ThrowIfZero(n)` | `n == 0` 抛 |
| `ArgumentOutOfRangeException.ThrowIfGreaterThan(n, max)` | `n > max` 抛，还有 `LessThan` 等 |
| `ObjectDisposedException.ThrowIf(disposed, this)` | 已释放还在用时抛 |

参数名是编译器通过 `[CallerArgumentExpression]` 自动填的，填的是**调用处写的表达式文本**。所以传变量时是变量名（`'name'`、`'amount'`），像上面 `ThrowIfNullOrEmpty("")` 直接传字面量时，参数名就成了 `'""'`。正常用法都是传参数变量。

```csharp
public void Withdraw(string account, decimal amount)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(account);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
    // ...
}
```

## 9. finally 与 return

```csharp
Console.WriteLine($"返回值 = {F()}");
Console.WriteLine($"返回值 = {G()}");
Console.WriteLine($"列表 = {string.Join(",", H())}");

static int F()
{
    int x = 1;
    try { return x; }
    finally { x = 2; Console.WriteLine($"finally 执行，x={x}"); }
}
static int G()
{
    try { throw new Exception("x"); }
    catch { Console.WriteLine("catch"); return -1; }
    finally { Console.WriteLine("finally 在 catch 的 return 之后、方法真正返回之前执行"); }
}
static List<int> H()
{
    var list = new List<int> { 1 };
    try { return list; }
    finally { list.Add(2); }
}
```
```
finally 执行，x=2
返回值 = 1
catch
finally 在 catch 的 return 之后、方法真正返回之前执行
返回值 = -1
列表 = 1,2
```

- `return x;` 时返回值已经算好（1），`finally` 里改 `x` 不影响返回值。
- 但返回的是引用类型时，`finally` 修改**对象内容**，调用方能看到（`1,2`）。
- 和 Java 的一个区别：**C# 的 `finally` 里不能写 `return`**（编译错误 CS0157）。Java 允许，而且会吞掉异常，是个坑；C# 直接禁止了。

## 10. IDisposable 与 using 🟢

### 10.1 为什么需要

GC 只管内存。文件句柄、数据库连接、网络连接这些**非内存资源**必须用完立刻释放，不能等 GC。约定是实现 `IDisposable` 接口，在 `Dispose()` 方法里释放：

```csharp
public interface IDisposable
{
    void Dispose();
}
```

相当于 Java 的 `AutoCloseable.close()`。.NET 里 `FileStream`、`StreamReader`、`HttpClient`、数据库连接 `DbConnection` 等都实现了 `IDisposable`。

### 10.2 using 语句 / using 声明

用一个会打印日志的资源类做实验：

```csharp
class Res(string name) : IDisposable
{
    private bool _disposed;
    public void Use()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Console.WriteLine($"  使用 {name}");
    }
    public void Dispose()
    {
        if (_disposed) return;          // 允许多次调用
        _disposed = true;
        Console.WriteLine($"  释放 {name}");
    }
}
```

```csharp
Console.WriteLine("== using 语句 ==");
using (var r = new Res("A"))
{
    r.Use();
}                                         // 出了大括号就 Dispose

Console.WriteLine("== using 声明 ==");
Demo();

static void Demo()
{
    using var r = new Res("B");           // 没有大括号，到所在作用域结束时 Dispose
    r.Use();
    Console.WriteLine("Demo 方法最后一行");
}
```
```
== using 语句 ==
  使用 A
  释放 A
== using 声明 ==
  使用 B
Demo 方法最后一行
  释放 B
```

出异常也会释放，多个资源按声明的**相反顺序**释放：

```csharp
try
{
    using var r = new Res("C");
    throw new InvalidOperationException("boom");
}
catch (Exception ex) { Console.WriteLine($"catch: {ex.Message}"); }

{
    using var r1 = new Res("D1");
    using var r2 = new Res("D2");
}
```
```
== 出异常也会 Dispose ==
  释放 C
catch: boom
== 多个资源，倒序释放 ==
  释放 D2
  释放 D1
```

注意 "释放 C" 在 "catch" 之前：`using` 本质是编译器生成的 `try/finally`，离开 `try` 块时就 `Dispose` 了。

| | C# | Java 7+ |
|---|---|---|
| 接口 | `IDisposable.Dispose()` | `AutoCloseable.close()` |
| 语法 | `using (var r = ...) { }` | `try (var r = ...) { }` |
| 简写 | `using var r = ...;`（到作用域结束） | 无 |
| 多个资源 | 多写几行 `using var` | `try (A a = ...; B b = ...)` |
| 释放顺序 | 倒序 | 倒序 |

Java 1.6 里要自己写 `finally { if (r != null) r.close(); }`，C# 的 `using` 把这些都包办了。

**日常优先用 `using var` 声明**，少一层缩进。需要提前释放（比如文件写完要马上再读）时用带大括号的 `using` 语句控制范围。

### 10.3 释放后再用

```csharp
var e = new Res("E");
e.Dispose(); e.Dispose();                 // 第二次什么都不做
try { e.Use(); } catch (ObjectDisposedException ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }
```
```
  释放 E
ObjectDisposedException: Cannot access a disposed object.
Object name: 'Res'.
```

自己实现 `IDisposable` 时的两条约定：

- `Dispose()` 可以被调用多次，第二次起什么都不做。
- 释放后再调用其他方法，抛 `ObjectDisposedException`。

### 10.4 Dispose 模式简介 ⚪

如果类**直接持有非托管资源**（操作系统句柄、指针），或者要设计成可被继承，官方有一个标准写法：

```csharp
class Base : IDisposable
{
    private bool _disposed;
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);        // 已经释放了，不需要终结器再来一遍
    }
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) Console.WriteLine("  Base: 释放托管资源");   // 其他 IDisposable 字段
        Console.WriteLine("  Base: 释放非托管资源");                // 句柄等
        _disposed = true;
    }
    ~Base() { Dispose(disposing: false); }   // 终结器：忘了 Dispose 时的兜底
}
class Derived : Base
{
    protected override void Dispose(bool disposing)
    {
        if (disposing) Console.WriteLine("  Derived: 释放自己的资源");
        base.Dispose(disposing);
    }
}
```

`using (var r = new Derived()) { }` 的输出：

```
  Derived: 释放自己的资源
  Base: 释放托管资源
  Base: 释放非托管资源
```

- `disposing = true`：是代码主动调 `Dispose()`，可以释放托管资源和非托管资源。
- `disposing = false`：是终结器调的，此时其他托管对象可能已经被回收，只能释放非托管资源。
- 终结器（`~Base()`，类似 Java 的 `finalize`）什么时候执行不确定。实测里没有 `Dispose` 的对象，`GC.Collect()` 之后到程序结束，终结器都没有打印。**不能依赖它**。

业务代码里绝大多数类只是**持有别的 `IDisposable` 对象**（比如一个 `HttpClient` 字段），不直接碰非托管资源，这时写一个简单的 `Dispose()` 方法（像上面 `Res` 那样）就够了，不需要终结器。

## 11. 小结

1. C# 没有受检异常，编译器不强制 `try/catch`；在能处理的地方捕获，不要到处吞异常。
2. 重新抛出用 `throw;`，**不要用 `throw ex;`**（会丢掉原始堆栈，和 Java 不同）；包装时把原异常作为 `innerException` 传进去。
3. 用 `catch (...) when (条件)` 按条件捕获，不满足的异常原样往外传。
4. 自定义异常继承 `Exception`，名字以 `Exception` 结尾，构造函数 `: base(message)`。
5. 预料之中的失败用 `TryParse` / `TryGetValue` 这类返回 `bool` 的方法，不要用异常做流程控制。
6. 参数校验用 `ArgumentNullException.ThrowIfNull`、`ArgumentOutOfRangeException.ThrowIfNegative` 等，参数名自动带上。
7. `finally` 里不能 `return`；`return` 的值在 `finally` 之前就算好了。
8. 实现了 `IDisposable` 的对象用 `using var` 声明，作用域结束自动释放，出异常也会释放，对应 Java 的 try-with-resources。

---

上一章：[集合与 LINQ 🟢](07-collections-linq.md) · [目录](../README.md) · 下一章：[现代 C# 语法 🟢](09-modern-syntax.md)
