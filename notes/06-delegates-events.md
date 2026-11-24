# 第 6 章 委托、Lambda 与事件 🟢

> 本章练习：[16-delegates.cs](../basics/16-delegates.cs)、[20-multicast-delegates.cs](../basics/20-multicast-delegates.cs)、[21-builtin-delegates.cs](../basics/21-builtin-delegates.cs)、[22-events.cs](../basics/22-events.cs)、[23-async-events.cs](../basics/23-async-events.cs)

> 在 JDK 1.6 里，想把"一段逻辑"传给别的方法，只能写一个接口再 `new` 一个匿名内部类。C# 从一开始就有**委托**，现在配合 Lambda，到处都在用：LINQ 的 `Where(x => ...)`、`List.Sort(...)`、`Task.Run(() => ...)`、ASP.NET Core 的 `app.MapGet("/", () => ...)`、按钮的 `Click` 事件……写代码时你会遇到这些问题：
> 看不懂 `Func<int, int, int>` 和 `Action<string>` 是什么；多播委托只拿到了最后一个返回值；循环里创建的 Lambda 打印出来全是同一个数；`event` 和普通委托字段看着一样，不知道区别在哪；页面关掉了内存却一直不释放；`async` 事件处理器没等执行完方法就返回了。
> 本章讲清楚：委托是什么、Lambda 怎么写、内置委托怎么选、多播委托的行为、闭包、事件的标准写法和常见坑。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 委托是什么

### 1.1 类型安全的方法引用

**委托（delegate）是"方法的类型"**。声明一个委托类型，就规定了"什么样签名的方法"可以放进来；委托变量就像一个指向方法的引用，可以赋值、当参数传、存进集合，然后调用。

和 Java 对比：

| | Java | C# |
|---|---|---|
| JDK 1.6 | 定义接口 + 匿名内部类：`new Comparator<String>() { public int compare(...) {...} }` | 委托 + 匿名方法 / Lambda |
| Java 8+ | 函数式接口（`Function<T,R>`、`Consumer<T>`、`Supplier<T>`、`Predicate<T>`）+ Lambda | 内置委托（`Func`、`Action`、`Predicate`）+ Lambda |
| 方法引用 | `this::increase`、`String::length` | 直接写方法名：`counter.Increase`（叫"方法组"） |
| 调用 | `fn.apply(x)`、`consumer.accept(x)`，每个接口方法名不同 | 统一像方法一样调用：`fn(x)`，或 `fn.Invoke(x)` |
| 本质 | 一个只有一个抽象方法的接口的实例 | 一个继承自 `System.MulticastDelegate` 的类的实例 |

"类型安全"的意思是：签名对不上就编译不过，不会到运行时才出错（和 C 的函数指针不同）。

### 1.2 声明、实例化、调用

```csharp
// 1. 声明委托类型：方法签名前面加 delegate，"方法名"位置就是新类型的名字
delegate int Multiple(int a, int b);

// 2. 三种赋值写法
Multiple m1 = Multiply;                                     // 方法组
Multiple m2 = delegate (int a, int b) { return a * b; };    // 匿名方法（C# 2.0 的老写法）
Multiple m3 = (a, b) => a * b;                              // Lambda（现在的主流写法）

Console.WriteLine($"2 * 3 = {m1(2, 3)}");
Console.WriteLine($"2 * 3 = {m2(2, 3)}");
Console.WriteLine($"2 * 3 = {m3(2, 3)}");

static int Multiply(int a, int b) => a * b;
```
```
2 * 3 = 6
2 * 3 = 6
2 * 3 = 6
```

还有一种完整写法 `new Multiple(Multiply)`，和 `Multiple m1 = Multiply;` 完全一样，老代码里常见。

调用有两种写法，完全等价：

```csharp
HelloWorldDelegate hello = Show;
hello();            // 像方法一样调用
hello.Invoke();     // 显式 Invoke；配合 ?. 时只能用这种：hello?.Invoke()
```
```
Hello World
Hello World
```

### 1.3 方法组会记住对象

把实例方法赋给委托时，委托同时记住了**方法**和**调用它的对象**：

```csharp
var counter = new Counter("A");
Action inc = counter.Increase;      // 实例方法组
inc();
inc();
Console.WriteLine($"counter.Value = {counter.Value}");
Console.WriteLine($"inc.Target = {inc.Target}, inc.Method = {inc.Method.Name}");
Console.WriteLine($"Action 的父类：{typeof(Action).BaseType}");

class Counter(string name)
{
    public int Value { get; private set; }
    public void Increase() => Value++;
    public override string ToString() => $"Counter({name})";
}
```
```
counter.Value = 2
inc.Target = Counter(A), inc.Method = Increase
Action 的父类：System.MulticastDelegate
```

`Target` 持有对象引用，这一点在第 7 节"事件导致的内存泄漏"里很关键。

### 1.4 委托作为参数：把"做什么"传进去

```csharp
Console.WriteLine($"Calc(6, 3, +) = {Calc(6, 3, (a, b) => a + b)}");
Console.WriteLine($"Calc(6, 3, -) = {Calc(6, 3, (a, b) => a - b)}");
Console.WriteLine($"Calc(6, 3, *) = {Calc(6, 3, Multiply)}");

static int Calc(int a, int b, Func<int, int, int> op) => op(a, b);
```
```
Calc(6, 3, +) = 9
Calc(6, 3, -) = 3
Calc(6, 3, *) = 18
```

这就是"策略模式"，Java 里要定义一个 `Operation` 接口和若干实现类，C# 一个委托参数就够了。

## 2. Lambda 表达式 🟢

### 2.1 写法

```
(参数) => 表达式
(参数) => { 语句; return 值; }
```

| 写法 | 例子 | 说明 |
|---|---|---|
| 无参数 | `() => Console.WriteLine("hi")` | 括号不能省 |
| 一个参数 | `x => x * 2` | 括号可以省 |
| 多个参数 | `(a, b) => a + b` | |
| 显式参数类型 | `(int a, int b) => a + b` | 一般靠推断，不用写 |
| 语句体 | `x => { var y = x * 2; return y; }` | 多行逻辑 |
| 丢弃参数 | `(_, _) => Console.WriteLine("不用参数")` | 事件处理器里常见 |
| static Lambda | `static (a, b) => a + b` | 保证不捕获外部变量，捕获了就编译报错 |

和 Java 8 的 Lambda 几乎一样，只是箭头是 `=>`（Java 是 `->`）。

```csharp
Func<int, int, int> add = static (a, b) => a + b;
Action<int, int> ignore = (_, _) => Console.WriteLine("两个参数都不用");
ignore(1, 2);
Console.WriteLine(add(1, 2));
```
```
两个参数都不用
3
```

`static` Lambda 里用了外部变量会直接报错：

```csharp
int x = 1;
Func<int> g = static () => x;
```
```
error CS8820: A static anonymous function cannot contain a reference to 'x'.
```

### 2.2 Lambda 可以用 var（C# 10+）

参数类型写明后，Lambda 有"自然类型"，可以直接 `var`，编译器推断成 `Func`/`Action`：

```csharp
var twice = (int x) => x * 2;
Console.WriteLine($"var 推断出的类型：{twice.GetType().Name}，twice(21) = {twice(21)}");
```
```
var 推断出的类型：Func`2，twice(21) = 42
```

`` Func`2 `` 就是 `Func<int, int>`（两个类型参数）。

## 3. 内置委托 Func / Action / Predicate 🟢

**绝大多数情况不需要自己写 `delegate` 声明**，用内置的就行。

### 3.1 速查表

| 委托 | 签名 | 记法 | Java 对应 |
|---|---|---|---|
| `Action` | `void ()` | 没参数、没返回值 | `Runnable` |
| `Action<T>` | `void (T)` | 有参数、没返回值 | `Consumer<T>` |
| `Action<T1, T2>` | `void (T1, T2)` | 最多 16 个参数 | `BiConsumer<T, U>` |
| `Func<TResult>` | `TResult ()` | 没参数、有返回值 | `Supplier<T>`、`Callable<V>` |
| `Func<T, TResult>` | `TResult (T)` | **最后一个类型参数是返回值** | `Function<T, R>` |
| `Func<T1, T2, TResult>` | `TResult (T1, T2)` | | `BiFunction<T, U, R>` |
| `Func<T, bool>` | `bool (T)` | LINQ 的 `Where` 用这个 | `Predicate<T>` |
| `Predicate<T>` | `bool (T)` | 老 API 用（`List.Find`、`Array.Find`） | `Predicate<T>` |
| `Comparison<T>` | `int (T, T)` | `List.Sort` 用 | `Comparator<T>` |
| `EventHandler` | `void (object? sender, EventArgs e)` | 事件标准签名 | 无 |
| `EventHandler<TEventArgs>` | `void (object? sender, TEventArgs e)` | 带自定义参数的事件 | 无 |

Java 为不同参数个数、基本类型准备了几十个接口（`IntFunction`、`ToLongBiFunction`……），C# 只有 `Func`/`Action` 两套，靠泛型参数个数区分，`int` 也能直接当类型参数。

⚠️ `Predicate<T>` 和 `Func<T, bool>` 签名一样，但**是两个不同的类型，不能互相赋值**。新代码统一用 `Func<T, bool>`，只有调用 `List.Find` 这类老 API 时才用 `Predicate<T>`。

```csharp
Predicate<int> p = n => n > 0;
Func<int, bool> f = p;
```
```
error CS0029: Cannot implicitly convert type 'System.Predicate<int>' to 'System.Func<int, bool>'
```

### 3.2 例子

```csharp
int[] numbers = [1, 2, 3, 4, 5];

// Action：没有返回值
Action action1 = () => Console.WriteLine("Hello World");
Action<int, int> action3 = (a, b) => Console.WriteLine($"{a} + {b} = {a + b}");
action1();
action3(1, 2);

// Func：最后一个类型参数是返回值
Func<int, int> square = x => x * x;
Func<int, int> cube = x => x * x * x;
var handlers = new List<Func<int, int>> { square, cube };      // 委托放进集合
foreach (var handler in handlers)
{
    Console.WriteLine(string.Join(", ", numbers.Select(handler)));   // Select 的参数就是 Func<T, TResult>
}

// 委托的参数也可以是委托
Func<Func<int>, string> check = Check;
Console.WriteLine(check(() => 3));

// Predicate：很多老 API 直接收它
Predicate<int> isEven = n => n % 2 == 0;
Console.WriteLine($"第一个偶数：{Array.Find(numbers, isEven)}");

static string Check(Func<int> func)
{
    int result = func();
    return $"{result} is {(result % 2 == 0 ? "even" : "odd")}";
}
```
```
Hello World
1 + 2 = 3
1, 4, 9, 16, 25
1, 8, 27, 64, 125
3 is odd
第一个偶数：2
```

⚠️ 不要自己定义名叫 `Action`、`Func` 的委托或类，会遮蔽 `System.Action`，导致 `Action<string>` 这样的写法报错。

### 3.3 什么时候还要自定义委托

- 参数有 `ref`/`out`/`in` 修饰（`Func`/`Action` 不支持）。
- 想要一个有业务含义的名字，让签名更好读（比如练习 25 里的 `DeleteHandler<T>`）。
- 老代码、框架 API 已经定义好了。

## 4. 多播委托 🟢

### 4.1 += 和 -=

一个委托变量可以挂多个方法，调用时**按添加顺序依次执行**：

```csharp
CalculateDelegate? calc = Add;
calc += Sub;
calc?.Invoke(2, 3);
Console.WriteLine("--------------------------");
calc -= Add;
calc?.Invoke(2, 3);   // 只剩 Sub

static void Add(int a, int b) => Console.WriteLine($"{a} + {b} = {a + b}");
static void Sub(int a, int b) => Console.WriteLine($"{a} - {b} = {a - b}");
delegate void CalculateDelegate(int a, int b);
```
```
2 + 3 = 5
2 - 3 = -1
--------------------------
2 - 3 = -1
```

- 全部 `-=` 掉之后变量就是 `null`，所以声明成可空、调用时用 `?.Invoke`。
- 同一个方法 `+=` 两次，会执行两次。
- 委托是**不可变**的：`+=` 实际上是生成一个新的委托对象再赋回变量，和 `string` 的 `+=` 类似。

### 4.2 ⚠️ -= Lambda 减不掉

`-=` 是按"同一个方法 + 同一个对象"匹配的。两段代码一样的 Lambda 是两个不同的委托：

```csharp
Action? log = () => Console.WriteLine("hi");
log -= () => Console.WriteLine("hi");     // 新建了另一个 lambda
Console.WriteLine($"-= 新 lambda 后：{log?.GetInvocationList().Length ?? 0} 个");

Action handler = () => Console.WriteLine("hi");
Action? log2 = handler;
log2 -= handler;                           // 同一个委托实例
Console.WriteLine($"-= 同一个变量后：{log2?.GetInvocationList().Length ?? 0} 个");

Action? log3 = Hello;
log3 -= Hello;                             // 方法组：同一个方法 + 同一个目标
Console.WriteLine($"-= 方法组后：{log3?.GetInvocationList().Length ?? 0} 个");
```
```
-= 新 lambda 后：1 个
-= 同一个变量后：0 个
-= 方法组后：0 个
```

需要取消订阅的，**用方法组或先把 Lambda 存到变量里**，不要直接 `+=` 一个匿名 Lambda。

### 4.3 返回值：只拿到最后一个

```csharp
Func<int> getNumber = () => { Console.WriteLine("执行 1"); return 1; };
getNumber += () => { Console.WriteLine("执行 2"); return 2; };
getNumber += () => { Console.WriteLine("执行 3"); return 3; };
Console.WriteLine($"返回值：{getNumber()}");

Console.WriteLine("--- 逐个调用，拿到所有返回值 ---");
foreach (Func<int> f in getNumber.GetInvocationList())
{
    Console.WriteLine($"返回值：{f()}");
}
```
```
执行 1
执行 2
执行 3
返回值：3
--- 逐个调用，拿到所有返回值 ---
执行 1
返回值：1
执行 2
返回值：2
执行 3
返回值：3
```

三个都执行了，但返回值**只保留最后一个**。需要所有结果时用 `GetInvocationList()` 拿到每个委托单独调用。所以多播一般只用于返回 `void` 的委托（事件就是这样）。

### 4.4 异常：中间一个抛了，后面的都不执行

```csharp
Action chain = () => Console.WriteLine("handler 1");
chain += () => throw new InvalidOperationException("handler 2 出错");
chain += () => Console.WriteLine("handler 3");
try
{
    chain();
}
catch (Exception ex)
{
    Console.WriteLine($"捕获：{ex.Message}");
}

Console.WriteLine("--- 逐个调用，互不影响 ---");
foreach (Action h in chain.GetInvocationList())
{
    try { h(); }
    catch (Exception ex) { Console.WriteLine($"捕获：{ex.Message}"); }
}
```
```
handler 1
捕获：handler 2 出错
--- 逐个调用，互不影响 ---
handler 1
捕获：handler 2 出错
handler 3
```

直接调用时 `handler 3` 没有执行。如果订阅者之间要互相隔离（一个出错不影响其他），触发方要用 `GetInvocationList()` 逐个 `try/catch`。

## 5. 闭包与循环变量捕获 🟢

### 5.1 闭包捕获的是变量，不是值

Lambda 可以使用外部的局部变量，这叫**捕获**，形成**闭包**。捕获的是**变量本身**，Lambda 里改了外面也变，外面改了 Lambda 里也看得到：

```csharp
int count = 0;
Action inc = () => count++;
inc();
inc();
Console.WriteLine($"count = {count}");

Func<int> makeCounter()
{
    int n = 0;
    return () => ++n;        // n 的生命周期被延长到和这个 lambda 一样长
}
var c1 = makeCounter();
var c2 = makeCounter();
Console.WriteLine($"c1: {c1()} {c1()} {c1()}, c2: {c2()}");
```
```
count = 2
c1: 1 2 3, c2: 1
```

和 Java 的区别：Java 的 Lambda/匿名内部类只能用 `final`（或事实上 final）的局部变量，**不能修改**；C# 可以随便读写。编译器在背后把被捕获的变量挪到了一个自动生成的类里。

### 5.2 ⚠️ for 循环变量捕获

```csharp
var actions = new List<Action>();
for (int i = 0; i < 3; i++)
{
    actions.Add(() => Console.Write($"{i} "));
}
foreach (var a in actions) a();
```
```
3 3 3
```

`for` 循环里的 `i` **整个循环只有一个变量**，三个 Lambda 捕获的都是它，等执行时循环已经结束，`i` 是 3。修复：在循环体里复制一份：

```csharp
for (int i = 0; i < 3; i++)
{
    int copy = i;                 // 每次循环都是一个新变量
    actions.Add(() => Console.Write($"{copy} "));
}
```
```
0 1 2
```

`foreach` 从 C# 5 开始每次迭代都是新变量，没有这个问题：

```csharp
foreach (var item in new[] { 0, 1, 2 })
{
    actions.Add(() => Console.Write($"{item} "));
}
```
```
0 1 2
```

| 循环 | 每次迭代是新变量吗 | Lambda 捕获的结果 |
|---|---|---|
| `for (int i = ...)` | ❌ 共用一个 `i` | 全是最终值 |
| `foreach (var x in ...)` | ✅（C# 5 起） | 各是各的 |

这个坑在多线程里最常见：`for` 循环里 `Task.Run(() => Process(i))`，见[第 12 章](12-task-parallel.md)。

## 6. 事件 event 🟢

### 6.1 event 和普通委托字段的区别

先看用普通委托字段做"订阅/通知"会出什么问题：

```csharp
var p = new Publisher();
p.FieldHandler += () => Console.WriteLine("订阅者 A");
p.FieldHandler += () => Console.WriteLine("订阅者 B");
p.FieldHandler = () => Console.WriteLine("外部用 = 覆盖了所有人");   // 把 A、B 都冲掉了
p.FieldHandler();                                                  // 外部直接触发
p.FieldHandler = null;                                             // 外部直接清空
Console.WriteLine($"外部置空后：{(p.FieldHandler is null ? "null" : "有值")}");

class Publisher
{
    public Action? FieldHandler;          // 普通委托字段
    public event Action? Changed;         // 事件
}
```
```
外部用 = 覆盖了所有人
外部置空后：null
```

任何外部代码都能覆盖、清空、冒充发布者触发。加上 `event` 关键字后，这些都不允许了：

```csharp
p.Changed += () => Console.WriteLine("ok");   // ✅ 只能 += / -=
p.Changed = () => Console.WriteLine("x");     // ❌
p.Changed();                                  // ❌
p.Changed?.Invoke();                          // ❌
```
```
error CS0070: The event 'Publisher.Changed' can only appear on the left hand side of += or -= (except when used from within the type 'Publisher')
```

| 操作（在类外部） | 委托字段 | event |
|---|---|---|
| `+=` / `-=` 订阅、取消 | ✅ | ✅ |
| `=` 赋值（覆盖所有订阅者） | ✅ 危险 | ❌ `CS0070` |
| 直接调用触发 | ✅ 危险 | ❌ `CS0070` |
| 置为 `null` | ✅ 危险 | ❌ `CS0070` |
| 在类内部触发 | ✅ | ✅ |

**`event` 就是"受保护的委托"：外部只能订阅和取消，只有类自己能触发。** 对外暴露通知机制一律用 `event`。

> Java 没有事件语法，一般是观察者模式：`addXxxListener(listener)` / `removeXxxListener(listener)`，内部维护一个 `List<Listener>`。C# 的 `event` 相当于编译器帮你生成了 `add`/`remove` 这两个方法。

### 6.2 标准写法：EventHandler<TEventArgs>

事件可以用任意委托类型（练习 22 里用了自定义委托、`Action`、`Func`、`Predicate`），但 .NET 的惯例是：

- 签名 `void (object? sender, XxxEventArgs e)`：`sender` 是触发事件的对象，`e` 是事件参数。
- 没有参数用 `EventHandler`，触发时传 `EventArgs.Empty`。
- 有参数就定义一个继承 `EventArgs`、以 `EventArgs` 结尾的类，用 `EventHandler<TEventArgs>`。

```csharp
var clock = new Clock();
clock.OnRing += (sender, e) => Console.WriteLine($"ring at {e.Time:HH:mm:ss}, sender: {sender}");
clock.Start();

// 1. 事件参数类：继承 EventArgs，类名以 EventArgs 结尾
class ClockEventArgs : EventArgs
{
    public DateTime Time { get; }
    public ClockEventArgs(DateTime time) => Time = time;
}

class Clock
{
    // 2. 声明事件：event + EventHandler<参数类型> + 名字；加 ? 因为没人订阅时是 null
    public event EventHandler<ClockEventArgs>? OnRing;

    public void Start()
    {
        // 3. 触发：sender 传 this
        OnRing?.Invoke(this, new ClockEventArgs(DateTime.Now));
    }
}
```
```
ring at 17:22:57, sender: Clock
```

订阅的几种写法（练习 22 的 `Button`）：

```csharp
button.Click += Button_Click;                         // 方法组（要取消订阅时用这个）
button.Click -= Button_Click;                         // 取消
button.Click += new EventHandler(Button_Click);       // 老写法
button.Click += (sender, e) => Console.WriteLine($"lambda: {sender}");
button.Click += delegate (object? sender, EventArgs e) { Console.WriteLine($"anonymous: {sender}"); };
button.Start();

static void Button_Click(object? sender, EventArgs e) => Console.WriteLine($"Button_Click: {sender}");
```
```
Button_Click: Button#1
lambda: Button#1
anonymous: Button#1
```

> 命名习惯：.NET 自己的类库里事件名一般是名词或动词（`Click`、`Changed`、`DataReceived`），处理方法叫 `对象名_事件名`（`Button_Click`）或 `OnXxx`。练习里的 `OnRing` 这种 `On` 前缀，在 .NET 惯例中通常留给"触发事件的 protected 方法"（`protected virtual void OnRing(...)`）。

### 6.3 ?.Invoke：没人订阅时不报错

没有任何订阅者时，事件的值是 `null`，直接调用会 `NullReferenceException`。用 `?.Invoke`：

```csharp
OnRing?.Invoke(this, args);      // 推荐
```

`?.` 只读取一次 `OnRing`，判空和调用用的是同一个值。老代码里常见的写法是先复制到局部变量再判断，效果一样：

```csharp
var handler = OnRing;            // C# 6 之前的写法
if (handler != null) handler(this, args);
```

不要写成 `if (OnRing != null) OnRing(this, args);`：多线程下，判断之后、调用之前，别的线程可能刚好 `-=` 掉最后一个订阅者，`OnRing` 变成 `null`。

### 6.4 静态事件

```csharp
StaticClock.OnRing += time => Console.WriteLine($"static ring at {time:HH:mm:ss}");
StaticClock.Start();

class StaticClock
{
    public static event Action<DateTime>? OnRing;
    public static void Start() => OnRing?.Invoke(DateTime.Now);
}
```
```
static ring at 17:22:57
```

⚠️ 静态事件活得和整个程序一样长，订阅了它的对象**永远不会被回收**，除非 `-=`。见下一节。

## 7. 事件导致的内存泄漏 ⚠️

`a.Event += b.Handler` 之后，`a` 内部的委托通过 `Target` 持有 `b` 的引用（见 1.3）。只要 `a` 还活着，`b` 就不会被 GC 回收。如果 `a` 是长寿命对象（单例、静态事件、全局消息总线），而 `b` 是短寿命对象（页面、请求、临时窗口），忘记 `-=` 就会泄漏。

用 `WeakReference` 观察对象有没有被回收：

```csharp
using System.Runtime.CompilerServices;

var bus = new EventBus();                  // 长寿命对象

var leaked = Subscribe(bus, unsubscribe: false);
var released = Subscribe(bus, unsubscribe: true);

GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

Console.WriteLine($"忘记 -= 的页面还活着吗：{leaked.IsAlive}");
Console.WriteLine($"-= 过的页面还活着吗：  {released.IsAlive}");
Console.WriteLine($"bus 上还挂着 {bus.SubscriberCount} 个订阅者");

[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference Subscribe(EventBus bus, bool unsubscribe)
{
    var page = new Page(bus);
    if (unsubscribe) page.Dispose();
    return new WeakReference(page);         // 弱引用：不阻止 GC 回收
}

class EventBus
{
    public event EventHandler? DataChanged;
    public int SubscriberCount => DataChanged?.GetInvocationList().Length ?? 0;
}

class Page : IDisposable
{
    private readonly EventBus _bus;
    private readonly byte[] _bigData = new byte[10_000_000];   // 10 MB，泄漏一个就是 10 MB

    public Page(EventBus bus)
    {
        _bus = bus;
        _bus.DataChanged += OnDataChanged;       // bus 持有了 this
    }

    private void OnDataChanged(object? sender, EventArgs e) { }

    public void Dispose() => _bus.DataChanged -= OnDataChanged;   // 取消订阅
}
```
```
忘记 -= 的页面还活着吗：True
-= 过的页面还活着吗：  False
bus 上还挂着 1 个订阅者
```

两个 `Page` 在方法返回后都没有任何变量引用了，但没 `-=` 的那个依然被 `bus` 拽着，连同它的 10 MB 数组一起回收不了。

怎么避免：

- **谁订阅，谁负责取消**。订阅方实现 `IDisposable`，在 `Dispose` 里 `-=`（`IDisposable` 见[第 8 章](08-exceptions-resources.md)）。
- 订阅时用方法组或保存 Lambda 变量，保证能 `-=`（见 4.2）。
- 发布者和订阅者寿命一样长（比如一个对象订阅自己的子控件），可以不取消。
- 对静态事件、单例上的事件要格外小心。

## 8. 异步事件 🟡

事件处理器里要 `await`（比如写数据库、调接口），可以把事件的委托类型定义成返回 `Task` 的：`Func<object, TEventArgs, Task>`。

⚠️ 坑：多播委托只返回**最后一个**订阅者的结果（见 4.3），对返回 `Task` 的委托也一样。直接 `await OnRing.Invoke(...)` 只等到了最后一个订阅者。

两个订阅者，分别耗时 1000 ms 和 300 ms，对比两种触发方式：

```csharp
clock.OnRing += async (s, e) => { await Task.Delay(1000); Console.WriteLine($"[{Sw.Ms}] 订阅者 1（1000ms）完成"); };
clock.OnRing += async (s, e) => { await Task.Delay(300);  Console.WriteLine($"[{Sw.Ms}] 订阅者 2（300ms）完成"); };

class Clock
{
    public event Func<object, EventArgs, Task>? OnRing;

    // ❌ 错误：只 await 了最后一个订阅者的 Task
    public async Task RingWrong()
    {
        if (OnRing is null) return;
        await OnRing.Invoke(this, EventArgs.Empty);
    }

    // ✅ 正确：逐个调用，拿到所有 Task，一起等
    public async Task Ring()
    {
        var handlers = OnRing;
        if (handlers is null) return;                 // 没人订阅：await null 会抛异常
        var tasks = handlers.GetInvocationList()
            .Cast<Func<object, EventArgs, Task>>()
            .Select(h => h(this, EventArgs.Empty));
        await Task.WhenAll(tasks);
    }
}
```
```
--- 直接 await Invoke ---
[ 300ms] 订阅者 2（300ms）完成
[ 300ms] RingWrong 返回了
[1000ms] 订阅者 1（1000ms）完成
--- GetInvocationList + Task.WhenAll ---
[ 300ms] 订阅者 2（300ms）完成
[1000ms] 订阅者 1（1000ms）完成
[1000ms] Ring 返回了
```

（时间取整到 100 ms。）`RingWrong` 在 300 ms 就返回了，订阅者 1 还没执行完；`Ring` 等到所有订阅者都完成才返回。各订阅者是并发执行的，总耗时是最慢的那个。

练习 23 的完整版本：

```csharp
await clock.Start();         // 必须一路 await 到顶层，否则程序直接结束，订阅者后半段来不及打印
Console.WriteLine("main end");
```
```
first subscriber start
second subscriber done
current time: 17:22:57
first subscriber end
main end
```

> 普通的 `EventHandler` 事件也能订阅 `async` Lambda：`button.Click += async (s, e) => await ...;`，但这时 Lambda 是 `async void`，触发方完全没法等它，里面抛的异常也接不住。`async void` 的问题见[第 13 章](13-async-await.md)。需要等待订阅者时，用本节返回 `Task` 的写法。

## 9. 小结

1. 委托是"方法的类型"，对应 Java 的函数式接口；赋值可以用方法组、Lambda、匿名方法，调用写 `f(x)` 或 `f.Invoke(x)`。
2. 一般不用自己声明 `delegate`：没返回值用 `Action<...>`，有返回值用 `Func<..., TResult>`（最后一个是返回值），判断用 `Func<T, bool>`。
3. 多播委托按顺序执行，返回值只保留最后一个，中间抛异常后面就不执行；要逐个处理用 `GetInvocationList()`。
4. `-=` 按"方法 + 对象"匹配，直接 `-=` 一个新写的 Lambda 减不掉。
5. 闭包捕获的是变量不是值；`for` 循环变量是共用的，Lambda 里要用就先复制一份，`foreach` 没这个问题。
6. 对外暴露通知用 `event`：外部只能 `+=`/`-=`；标准写法是 `EventHandler<TEventArgs>` + `?.Invoke(this, e)`。
7. 短寿命对象订阅长寿命对象的事件，一定要 `-=`，否则内存泄漏。
8. 异步事件用返回 `Task` 的委托，触发时 `GetInvocationList()` + `Task.WhenAll`，不要直接 `await Invoke`。

---

上一章：[泛型 🟢](05-generics.md) · [目录](../README.md) · 下一章：[集合与 LINQ 🟢](07-collections-linq.md)
