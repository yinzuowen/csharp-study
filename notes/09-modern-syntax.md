# 第 9 章 现代 C# 语法 🟢

> 本章练习：[24-tuples.cs](../basics/24-tuples.cs)

> 写代码时你会遇到这些问题：
> 一个方法想返回两个值，难道要专门写个类？DTO 类写了一堆属性，还要手写 `Equals`、`GetHashCode`、`ToString`；一长串 `if (x is A) ... else if` 判断类型和字段；编译器一直报 `CS8602`、`CS8618` 黄色警告，不知道怎么消；别人代码里的 `""" ... """`、`#:package`、`namespace Xxx;` 看不懂。
> 本章讲 C# 7 以后加入的常用语法：元组、record、模式匹配、可空引用类型、顶层语句与 file-based app、全局 using、原始字符串。这些在 Java 1.6 里都没有，但在现在的 C# 代码里到处都是。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 元组与解构

### 1.1 值元组：方法返回多个值

练习 24：

```csharp
// System.Tuple：class，只读，成员名只能是 Item1、Item2…（老写法，不推荐）
var obj1 = Tuple.Create(1, 2, 3);
Console.WriteLine($"{obj1}, Item1={obj1.Item1}");

// 值元组 ValueTuple：struct，可读写，可以给成员起名（推荐）
(string Name, int Age) obj4 = ("Zoe", 10);
obj4.Age++;
Console.WriteLine($"Name={obj4.Name}, Age={obj4.Age}");

// 最常见用途：方法返回多个值（代替 out 参数）
List<int> list = [1, 2, 4, 8, 16];
var result = MinMax(list);
Console.WriteLine($"min={result.min}, max={result.max}");

var (min, max) = MinMax(list);     // 解构：直接拆成两个变量
Console.WriteLine($"min={min}, max={max}");

int a = 1, b = 2;
(a, b) = (b, a);                   // 交换两个变量，不需要临时变量
Console.WriteLine($"a={a}, b={b}");

static (int min, int max) MinMax(List<int> list)
{
    int min = int.MaxValue;
    int max = int.MinValue;
    foreach (var item in list)
    {
        if (item < min) min = item;
        if (item > max) max = item;
    }
    return (min, max);
}
```
```
(1, 2, 3), Item1=1
Name=Zoe, Age=11
min=1, max=16
min=1, max=16
a=2, b=1
```

| | `Tuple<T1,T2>`（老） | `(T1, T2)` 值元组（新） |
|---|---|---|
| 类型 | class（引用类型） | struct（值类型） |
| 成员名 | 只能 `Item1`、`Item2` | 可以起名：`(int min, int max)` |
| 可变 | 只读 | 可读写 |
| 用不用 | 不用 | 用 |

Java 里没有元组，要么返回数组/`Map`，要么专门写一个类。C# 里**方法内部、私有方法之间**传两三个值，用元组很方便；如果是公开 API、字段多、要到处传，还是定义 `record`（第 2 节）更清楚。

### 1.2 弃元 _ 与更多解构

```csharp
var t = (Name: "Zoe", Age: 10);
var (name, _) = t;                  // 弃元：不要 Age
Console.WriteLine(name);
Console.WriteLine((1, "a") == (1, "a"));   // 元组按值比较
_ = int.TryParse("42", out _);      // 只关心是否成功，不要结果
var p = new Point(3, 4);
var (x, y) = p;                     // 自定义类也能解构
Console.WriteLine($"x={x} y={y}");
var dict = new Dictionary<string, int> { ["a"] = 1 };
foreach (var (k, v) in dict) Console.WriteLine($"{k}={v}");   // KeyValuePair 自带 Deconstruct
var named = (t.Name, t.Age);        // 成员名自动推断为 Name、Age
Console.WriteLine(named.Name);

class Point(int x, int y)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public void Deconstruct(out int x, out int y) { x = X; y = Y; }   // 有这个方法就能解构
}
```
```
Zoe
True
x=3 y=4
a=1
Zoe
```

`_` 叫**弃元**（discard），表示"这个值我不要"。出现在解构、`out _`、`_ = 方法()`（明确表示忽略返回值）、`switch` 的默认分支 `_ =>` 里。

## 2. record 🟢

### 2.1 record class：一行写完一个数据类

```csharp
record Person(string Name, int Age);
```

这一行，编译器生成了：两个 `init` 只读属性 `Name`、`Age`；构造函数；**按值比较**的 `Equals`、`GetHashCode`、`==`；可读的 `ToString`；`Deconstruct`；支持 `with` 表达式。

```csharp
var p1 = new Person("Tom", 18);
var p2 = new Person("Tom", 18);
Console.WriteLine(p1);
Console.WriteLine($"p1 == p2: {p1 == p2}, Equals: {p1.Equals(p2)}, ReferenceEquals: {ReferenceEquals(p1, p2)}");
Console.WriteLine($"HashCode 相同: {p1.GetHashCode() == p2.GetHashCode()}");
var p3 = p1 with { Age = 19 };     // 复制一份，改 Age
Console.WriteLine($"{p3}  p1 不变: {p1}");
// p1.Age = 20;   // 编译错误 CS8852：init-only 属性只能在初始化时赋值
var (n, a) = p1;
Console.WriteLine($"{n} {a}");
var set = new HashSet<Person> { p1, p2 };
Console.WriteLine($"HashSet.Count = {set.Count}");

// 对比：普通 class
var c1 = new PersonClass("Tom", 18);
var c2 = new PersonClass("Tom", 18);
Console.WriteLine($"{c1}  c1 == c2: {c1 == c2}");

class PersonClass(string name, int age) { public string Name => name; public int Age => age; }
```
```
Person { Name = Tom, Age = 18 }
p1 == p2: True, Equals: True, ReferenceEquals: False
HashCode 相同: True
Person { Name = Tom, Age = 19 }  p1 不变: Person { Name = Tom, Age = 18 }
Tom 18
HashSet.Count = 1
PersonClass  c1 == c2: False
```

| | 普通 class | record class |
|---|---|---|
| `==` / `Equals` | 比较引用 | 比较所有属性的值 |
| `ToString()` | 类名 `PersonClass` | `Person { Name = Tom, Age = 18 }` |
| 属性 | 自己写 | 主构造参数自动生成 `init` 属性 |
| 修改 | 直接改属性 | 不可变，用 `with` 复制出新对象 |
| 适合 | 有行为、有状态的对象（Service、实体） | DTO、值对象、配置、消息、返回结果 |

`with` 对普通 class 不能用（编译错误 CS8858），只能用于 record 和 struct。

### 2.2 和 Java record 对比

Java 16 才加了 `record`，写法几乎一样：

```java
record Person(String name, int age) {}          // Java
p.name()                                         // 访问用方法
```

```csharp
record Person(string Name, int Age);             // C#
p.Name                                           // 访问用属性
```

| | C# record | Java record |
|---|---|---|
| 值相等、`ToString` | 有 | 有 |
| `with` 复制 | 有 | 没有（要自己 `new`） |
| 继承 | record 可以继承 record | 不能继承 |
| 解构 | `var (n, a) = p;` | Java 21 起可以在模式匹配里解构 |
| 也能写成 struct | `record struct` | 没有 |

### 2.3 record struct 与其他写法

```csharp
var pt = new Pt(1, 2);
pt.X = 10;                       // record struct 默认是可变的！
Console.WriteLine(pt);
var rpt = new RPt(1, 2);
Console.WriteLine(rpt with { Y = 5 });

var u = new User { Name = "Joe", Email = "j@x.com" };
Console.WriteLine(u);

record struct Pt(int X, int Y);
readonly record struct RPt(int X, int Y);     // 加 readonly 才不可变
record User                                   // 不用主构造，自己写属性
{
    public required string Name { get; init; }
    public string? Email { get; init; }
}
```
```
Pt { X = 10, Y = 2 }
RPt { X = 1, Y = 5 }
User { Name = Joe, Email = j@x.com }
```

⚠️ `record class` 的主构造属性是 `init`（不可改），`record struct` 的是 `set`（可改），想要不可变要写 `readonly record struct`。

### 2.4 ⚠️ record 的几个坑

```csharp
var o1 = new Order(1, ["a"]);
var o2 = new Order(1, ["a"]);
Console.WriteLine($"含 List 的 record 相等: {o1 == o2}");
var o3 = o1 with { };
o3.Items.Add("b");
Console.WriteLine($"with 是浅拷贝: o1.Items = {string.Join(",", o1.Items)}");

var e = new Employee("Amy", 30, "IT");
Console.WriteLine(e);
Person asPerson = e;
Console.WriteLine($"Employee == Person(同值): {asPerson == new Person("Amy", 30)}");

record Order(int Id, List<string> Items);
record Employee(string Name, int Age, string Dept) : Person(Name, Age);
```
```
含 List 的 record 相等: False
with 是浅拷贝: o1.Items = a,b
Employee { Name = Amy, Age = 30, Dept = IT }
Employee == Person(同值): False
```

1. 值相等是对每个属性调 `Equals`，`List` 比较的是引用，所以两个内容一样的 `Order` 不相等。
2. `with` 是**浅拷贝**，`o3` 和 `o1` 共用同一个 `List`。（深浅拷贝见[第 2 章](02-value-and-reference.md)。）
3. 相等还要求**运行时类型相同**，`Employee` 和 `Person` 即使共有属性一样也不相等。

## 3. 模式匹配 🟢

模式匹配就是"判断一个值是不是某种形状，是的话顺便取出里面的东西"。用在 `is` 表达式、`switch` 语句和 `switch` 表达式里。

### 3.1 switch 表达式 + 类型模式

```csharp
object[] items = [42, "hello", 3.14, null!, new List<int> { 1, 2 }, -7];
foreach (var o in items) Console.WriteLine(Describe(o));

static string Describe(object? o) => o switch
{
    null => "null",
    int n when n < 0 => $"负整数 {n}",              // 类型模式 + when 条件
    int n => $"整数 {n}",
    string { Length: > 3 } s => $"长字符串 {s}",     // 类型 + 属性模式
    string s => $"字符串 {s}",
    double d => $"double {d}",
    IList<int> { Count: var cnt } => $"int 列表，{cnt} 个元素",
    _ => "其他",                                     // 弃元 = default
};
```
```
整数 42
长字符串 hello
double 3.14
null
int 列表，2 个元素
负整数 -7
```

`switch` 表达式和 Java 1.6 的 `switch` 语句对比：

| | Java 1.6 `switch` 语句 | C# `switch` 表达式 |
|---|---|---|
| 能 switch 的类型 | int、char、enum（Java 7 起支持 String） | 任意类型 |
| 有返回值 | 没有 | 有，`var x = v switch { ... };` |
| `break` | 必须写，忘了就穿透 | 没有 `break`，不会穿透 |
| 分支条件 | 只能是常量 | 类型、属性、范围、`when` 条件…… |
| 默认 | `default:` | `_ =>` |

（Java 14 以后也有了 `switch` 表达式，Java 21 有了模式匹配，思路一样。）

分支**从上往下匹配**，所以具体的写前面（`int n when n < 0` 在 `int n` 前面）。如果前面的分支已经覆盖了后面的，编译器报错 CS8510（The pattern is unreachable）。

### 3.2 is 表达式

```csharp
if (obj is string s) Console.WriteLine(s.Length);   // 判断 + 转换一步完成
if (str is not null) { ... }                        // 推荐的判空写法
```

Java 1.6 里要写 `if (obj instanceof String) { String s = (String) obj; ... }`，C# 一步完成（Java 16 起也有了 `instanceof String s`）。

### 3.3 属性模式

按对象的属性值匹配，可以嵌套：

```csharp
Order[] orders = [new(1, 50, "CN", true), new(2, 500, "US", false), new(3, 5000, "CN", false)];
foreach (var o in orders) Console.WriteLine($"订单 {o.Id}: 运费 {Shipping(o)}");

static decimal Shipping(Order o) => o switch
{
    { Vip: true } => 0,
    { Country: "CN", Amount: >= 1000 } => 0,
    { Country: "CN" } => 10,
    _ => 50,
};
record Order(int Id, decimal Amount, string Country, bool Vip);
```
```
订单 1: 运费 0
订单 2: 运费 50
订单 3: 运费 0
```

属性模式还隐含了"不是 `null`"：`o is { }` 等价于 `o is not null`。

### 3.4 关系模式与逻辑模式 and / or / not

```csharp
foreach (var s in new[] { -1, 0, 59, 60, 85, 100, 101 }) Console.WriteLine($"{s} -> {Grade(s)}");
char c = 'k';
Console.WriteLine(c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'));
string? str = null;
if (str is not null) Console.WriteLine("not null"); else Console.WriteLine("is null");

static string Grade(int s) => s switch
{
    < 0 or > 100 => "非法",
    >= 85 => "优",
    >= 60 => "及格",
    _ => "不及格",
};
```
```
-1 -> 非法
0 -> 不及格
59 -> 不及格
60 -> 及格
85 -> 优
100 -> 优
101 -> 非法
True
is null
```

- 关系模式：`< 0`、`>= 85`，右边必须是常量。
- 逻辑模式：`and`、`or`、`not`（是单词，不是 `&&`、`||`、`!`），只能用在模式里。
- `x is >= 1 and <= 10` 比 `x >= 1 && x <= 10` 少写一次变量名。

### 3.5 列表模式

匹配数组 / 列表的形状，`..` 表示"任意个元素"：

```csharp
int[][] arrays = [[], [1], [1, 2], [1, 2, 3, 4], [0, 9]];
foreach (var a in arrays) Console.WriteLine($"[{string.Join(",", a)}] -> {Shape(a)}");
string[] cmd = ["git", "commit", "-m", "msg"];
if (cmd is ["git", "commit", .. var rest]) Console.WriteLine($"commit 参数: {string.Join(" ", rest)}");

static string Shape(int[] a) => a switch
{
    [] => "空",
    [var x] => $"只有一个 {x}",
    [0, ..] => "以 0 开头",
    [var first, .., var last] => $"首 {first} 尾 {last}",
};
```
```
[] -> 空
[1] -> 只有一个 1
[1,2] -> 首 1 尾 2
[1,2,3,4] -> 首 1 尾 4
[0,9] -> 以 0 开头
commit 参数: -m msg
```

解析命令行参数、处理固定格式的数据时很好用。

### 3.6 元组模式

同时匹配多个值，把它们拼成元组再 `switch`：

```csharp
Console.WriteLine(Rps("石头", "剪刀"));
Console.WriteLine(Rps("布", "剪刀"));

static string Rps(string a, string b) => (a, b) switch
{
    ("石头", "剪刀") or ("剪刀", "布") or ("布", "石头") => "a 赢",
    var (x, y) when x == y => "平",
    _ => "b 赢",
};
```
```
a 赢
b 赢
```

### 3.7 ⚠️ switch 表达式没覆盖所有情况

```csharp
static string DayName(int d) => d switch { 1 => "Mon", 2 => "Tue" };
Console.WriteLine(DayName(8));
```

编译时警告：

```
warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '0' is not covered.
```

运行时传入没覆盖的值，抛 `SwitchExpressionException`。**`switch` 表达式最后一般都加 `_ =>` 分支**（返回默认值或抛 `ArgumentOutOfRangeException`），不要忽略 CS8509 警告。

## 4. 可空引用类型 🟢

### 4.1 string 和 string?

Java 里任何引用都可能是 `null`，只能靠自觉和注解。C# 8 起加了**可空引用类型**，新项目和 file-based app 默认开启（`<Nullable>enable</Nullable>`）：

| 写法 | 含义 |
|---|---|
| `string s` | 承诺"不会是 `null`"，编译器检查你别把 `null` 赋给它 |
| `string? s` | 可能是 `null`，编译器检查你用之前判空 |

⚠️ 这只是**编译期检查，给出警告**，不改变运行时行为：`string` 变量运行时照样可以是 `null`，也照样会 `NullReferenceException`。

（`int?` 是另一回事：它是 `Nullable<int>` 值类型，运行时真的不同，见[第 1 章](01-types-and-syntax.md)。）

### 4.2 常见警告

```csharp
string? a = Environment.GetEnvironmentVariable("NOT_EXIST");   // 返回 string?
string? b = Environment.GetEnvironmentVariable("NOT_EXIST");
string? c = Environment.GetEnvironmentVariable("NOT_EXIST");
string s1 = a;                             // CS8600
Print(b);                                  // CS8604
Console.WriteLine(c.Length);               // CS8602

static string Find(int id)
{
    return id > 0 ? null : "x";            // CS8603
}
static void Print(string s) => Console.WriteLine(s);

class User
{
    public string Name { get; set; }       // CS8618
}
```
```
nullable.cs(16,19): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable.
nullable.cs(4,13): warning CS8600: Converting null literal or possible null value to non-nullable type.
nullable.cs(5,7): warning CS8604: Possible null reference argument for parameter 's' in 'void Print(string s)'.
nullable.cs(6,19): warning CS8602: Dereference of a possibly null reference.
nullable.cs(10,12): warning CS8603: Possible null reference return.

Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
```

警告归警告，程序照样能编译运行，然后在第 6 行 `c.Length` 崩了。这些警告**就是在提前告诉你哪里会 NRE**。

| 警告 | 意思 | 怎么正确消除 |
|---|---|---|
| CS8600 | 把可能为 `null` 的值赋给非空变量 | 变量改成 `string?`；或用 `?? 默认值`；或先判空 |
| CS8602 | 对可能为 `null` 的变量 `.` 成员 | 先判空（`if (x is not null)`）；或用 `x?.Length` |
| CS8603 | 非空返回类型的方法可能返回 `null` | 返回类型改成 `string?`，让调用方处理；或者确实不该为 `null` 就抛异常 |
| CS8604 | 把可能为 `null` 的值传给非空参数 | 调用前判空；或参数改成 `string?` 并在方法里处理 |
| CS8618 | 非空属性/字段在构造结束时没赋值 | 构造函数里赋值；加 `required`；给默认值 `= "";`；或真的可空就改 `string?` |

### 4.3 正确的处理方式

```csharp
string? env = Environment.GetEnvironmentVariable("NOT_EXIST");

// 1. 判空后编译器知道它不是 null（流分析）
if (env is not null) Console.WriteLine(env.Length);
// 2. ?. 和 ??
Console.WriteLine(env?.Length ?? -1);
string s1 = env ?? "default";
Console.WriteLine(s1);
// 3. 不该为 null 就尽早抛
try
{
    string s2 = env ?? throw new InvalidOperationException("缺少环境变量 NOT_EXIST");
}
catch (Exception ex) { Console.WriteLine(ex.Message); }
// 4. TryGetValue 的 out 参数带 [MaybeNullWhen(false)] 标注，返回 true 的分支里不会警告
var dict = new Dictionary<string, string> { ["k"] = "v" };
if (dict.TryGetValue("k", out var v)) Console.WriteLine(v.Length);
// 5. ! 只是让编译器闭嘴
string s3 = env!;
try { Console.WriteLine(s3.Length); } catch (NullReferenceException) { Console.WriteLine("用了 ! 照样 NullReferenceException"); }
Console.WriteLine(Find(1) ?? "没找到");
var u = new User { Name = "Tom" };
Console.WriteLine(u.Name);

static string? Find(int id) => id > 0 ? null : "x";   // 可能找不到，就老实声明成 string?
class User
{
    public required string Name { get; set; }        // required：创建时必须赋值，消除 CS8618
    public string? Nickname { get; set; }            // 真的可以没有
}
```
```
-1
default
缺少环境变量 NOT_EXIST
1
用了 ! 照样 NullReferenceException
没找到
Tom
```

### 4.4 ⚠️ ! 运算符慎用

`x!`（null-forgiving，"null 宽恕运算符"）告诉编译器"我保证它不是 `null`"，警告消失，但**运行时什么都不检查**。上面 `s3` 就是例子：警告没了，异常还在。

可以用 `!` 的场景很少：

- 你比编译器知道得多，而且有把握。比如 `ll.Find(2)!`（[第 7 章](07-collections-linq.md)里刚 `new` 的链表，确定有 2）。
- 测试代码。

**不要为了消警告到处加 `!`**。先想清楚这个值到底可不可能是 `null`：可能，就改类型为 `T?` 并处理；不可能，就在源头保证（构造函数赋值、`required`、`?? throw`）。

## 5. 顶层语句与 file-based app

### 5.1 顶层语句

`basics/` 下的文件都是这种写法：直接写语句，不写 `class Program` 和 `Main`。

```csharp
Console.WriteLine("hello");          // 编译器自动生成 Program 类和 Main 方法
Console.WriteLine(args.Length);      // 命令行参数直接用 args
static int Add(int a, int b) => a + b;   // 这里写的方法是 Main 里的本地函数
class Foo { }                        // 类型声明必须写在所有语句之后
```

规则：一个项目只能有一个文件用顶层语句；类型（`class`、`record`）写在语句后面。

`advanced/` 下用的是传统写法（`namespace HelloWorld` + `internal class Program` + `static void Main`）。两种写法编译结果一样，选一种就行。前面异常堆栈里的 `Program.<Main>$`、`g__Inner|0_0` 就是编译器为顶层语句和本地函数生成的名字。

### 5.2 file-based app 与 #: 指令

.NET 10 起，一个 `.cs` 文件不需要 `.csproj` 就能直接运行：`dotnet run xxx.cs`。原来要写在 `.csproj` 里的配置，用文件开头的 `#:` 指令写：

```csharp
#!/usr/bin/env dotnet
#:property PublishAot=false
global using System.Text;
using static System.Math;
using Json = System.Text.Json.JsonSerializer;

var sb = new StringBuilder("hello");
Console.WriteLine(sb.Append('!'));
Console.WriteLine(Sqrt(16) + Max(1, 2));
Console.WriteLine(Json.Serialize(new { A = 1 }));
Console.WriteLine(args.Length > 0 ? string.Join(",", args) : "无参数");
```
```
$ dotnet run directives.cs -- a b
hello!
6
{"A":1}
a,b
```

| 指令 | 作用 | 对应 `.csproj` |
|---|---|---|
| `#:property 名=值` | 设置 MSBuild 属性 | `<PropertyGroup><名>值</名></PropertyGroup>` |
| `#:package 包名@版本` | 引用 NuGet 包 | `<PackageReference Include=... Version=... />` |
| `#:sdk 名` | 换 SDK，如 `Microsoft.NET.Sdk.Web` | `<Project Sdk="...">` |
| `#:project 路径` | 引用另一个项目 | `<ProjectReference />` |
| `#!/usr/bin/env dotnet` | shebang，`chmod +x` 后可以 `./xxx.cs` 直接运行 | — |

为什么 `basics/` 的文件第一行是 `#:property PublishAot=false`：file-based app **默认开启 AOT 相关设置**，基于反射的 JSON 序列化被禁用。去掉这一行，实测：

```
warning IL2026: Using member 'System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)' which has 'RequiresUnreferencedCodeAttribute' ...
warning IL3050: ... 
Unhandled exception. System.InvalidOperationException: Reflection-based serialization has been disabled for this application. ...
```

所以用到反射、`JsonSerializer` 的单文件，要加 `#:property PublishAot=false`。

参数要放在 `--` 后面传给程序：`dotnet run xxx.cs -- a b`。

`#:package` 需要联网下载 NuGet 包（实测在没有网络的环境里 restore 失败，报 `NU1301`）。

文件变大、多文件、要写测试时，就该建正式项目了（见[第 15 章](15-engineering.md)）。

## 6. using 指令：全局 using 与隐式 using

### 6.1 隐式 using

新项目（`<ImplicitUsings>enable</ImplicitUsings>`）和 file-based app 会自动加一批 `global using`，所以 `Console`、`List<T>`、LINQ、`File`、`Task` 都不用写 `using`。实测 file-based app 自动生成的 `GlobalUsings.g.cs`：

```csharp
// <auto-generated/>
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Net.Http.Json;
global using System.Threading;
global using System.Threading.Tasks;
```

⚠️ **常用的 `System.Text`、`System.Reflection`、`System.Text.Json`、`System.Diagnostics`、`System.Globalization` 都不在里面**：

```csharp
var sb = new StringBuilder();       // 没写 using System.Text;
Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Name);   // 没写 using System.Reflection;
```
```
error CS0246: The type or namespace name 'StringBuilder' could not be found (are you missing a using directive or an assembly reference?)
error CS0103: The name 'Assembly' does not exist in the current context
```

看到 CS0246 / CS0103 说某个类型找不到，先想是不是缺 `using`。

| 类型 | 需要的 using |
|---|---|
| `StringBuilder`、`Encoding` | `System.Text` |
| `Regex` | `System.Text.RegularExpressions` |
| `JsonSerializer` | `System.Text.Json` |
| `Assembly`、`PropertyInfo`、`BindingFlags` | `System.Reflection` |
| `Stopwatch`、`Debug` | `System.Diagnostics` |
| `CultureInfo` | `System.Globalization` |
| `ConcurrentDictionary` | `System.Collections.Concurrent` |

### 6.2 全局 using 与其他 using 写法

```csharp
global using System.Text;                          // 对整个项目所有文件生效
using static System.Math;                          // 之后可以直接写 Sqrt(16)、Max(1, 2)
using Json = System.Text.Json.JsonSerializer;      // 别名
```

正式项目里一般建一个 `GlobalUsings.cs` 集中放 `global using`，或者在 `.csproj` 里写 `<Using Include="System.Text" />`。

和 Java 对比：C# 的 `using` 导入的是**命名空间**（相当于 Java 的 `import java.util.*;`），没有只导入一个类的写法（别名 `using X = ...;` 除外）；`using static` 相当于 Java 的 `import static`。

## 7. 文件作用域 namespace

```csharp
// 老写法：多一层大括号和缩进
namespace Demo
{
    class A { }
}

// 新写法（C# 10）：整个文件都属于这个 namespace
namespace Demo;

internal class Program
{
    static void Main() => Console.WriteLine(typeof(Program).FullName);
}
```
```
Demo.Program
```

正式项目里的文件基本都用文件作用域 namespace，少一层缩进。

⚠️ 不能和顶层语句写在同一个文件里：

```
error CS8956: File-scoped namespace must precede all other members in a file.
```

## 8. 目标类型 new()

左边已经写了类型，右边 `new` 后面可以省略类型名：

```csharp
List<int> list = new();
Dictionary<string, List<int>> map = new() { ["a"] = new() { 1, 2 } };
Point p = new(1, 2);
Console.WriteLine($"{list.Count} {map["a"].Count} {p}");
Show(new(3, 4));                          // 参数类型已知，也能省略

static void Show(Point p) => Console.WriteLine(p);
record Point(int X, int Y);
```
```
0 2 Point { X = 1, Y = 2 }
Point { X = 3, Y = 4 }
```

和 `var` 是两个方向的省略：`var list = new List<int>();` 省左边，`List<int> list = new();` 省右边。**字段和属性**不能用 `var`，所以常用 `new()`：

```csharp
private readonly Dictionary<string, int> _cache = new();
```

## 9. 字符串

### 9.1 原始字符串字面量 """

三个及以上双引号括起来，里面的 `\`、`"` 都不用转义，适合写 JSON、SQL、正则：

```csharp
string json = """
    {
      "name": "Tom",
      "path": "C:\temp\new"
    }
    """;
Console.WriteLine(json);
string sql = """select * from t where name = 'a"b'""";
Console.WriteLine(sql);
```
```
{
  "name": "Tom",
  "path": "C:\temp\new"
}
select * from t where name = 'a"b'
```

- 多行时，开头 `"""` 后面直接换行，结尾 `"""` 单独一行；**结尾 `"""` 前面的缩进会从每一行去掉**，所以代码可以正常缩进。
- 带插值时，`$` 的个数 = 插值要用几个 `{`。JSON 里本来就有 `{`，用 `$$`，插值写 `{{...}}`：

```csharp
string json2 = $$"""
    {"id": {{id}}, "name": "{{name}}"}
    """;
```
```
{"id": 42, "name": "Tom"}
```

其他字符串写法：

| 写法 | 含义 |
|---|---|
| `"a\tb"` | 普通字符串，`\` 转义 |
| `@"C:\temp\new ""quoted"""` | 逐字字符串：`\` 不转义，`""` 表示一个 `"` |
| `$"{name}"` | 插值字符串，相当于 Java 的 `String.format` |
| `$@"{name}: C:\Users\{name}"` | 插值 + 逐字 |
| `"""..."""` | 原始字符串 |

### 9.2 插值格式化

`{表达式,对齐:格式}`：

```csharp
decimal price = 1234.5m;
double rate = 0.1234;
int id = 42;
DateTime dt = new(2026, 11, 21, 9, 5, 3);
Console.WriteLine($"[{price:N2}] [{price:F0}] [{rate:P1}] [{id:D5}] [{id:X}] [{255:x4}]");
Console.WriteLine($"[{name,8}] [{name,-8}] [{price,10:N1}]");
Console.WriteLine($"{dt:yyyy-MM-dd HH:mm:ss} {dt:yyyy/M/d}");
Console.WriteLine($"{(id > 40 ? "big" : "small")}");   // 三元表达式要加括号（: 会被当成格式）
Console.WriteLine($"{{花括号}} {name}");                // {{ }} 输出花括号本身
```
```
[1,234.50] [1235] [12.3%] [00042] [2A] [00ff]
[     Tom] [Tom     ] [   1,234.5]
2026-11-21 09:05:03 2026/11/21
big
{花括号} Tom
```

| 格式 | 含义 | 例子 → 结果 |
|---|---|---|
| `N2` | 千分位 + 2 位小数 | `1234.5` → `1,234.50` |
| `F0` | 固定小数位 | `1234.5m` → `1235` |
| `P1` | 百分比 | `0.1234` → `12.3%` |
| `D5` | 整数补零到 5 位 | `42` → `00042` |
| `X` / `x4` | 十六进制 | `42` → `2A`，`255` → `00ff` |
| `,8` / `,-8` | 右对齐 / 左对齐，宽度 8 | `Tom` → `"     Tom"` / `"Tom     "` |
| `yyyy-MM-dd HH:mm:ss` | 日期时间 | `2026-11-21 09:05:03` |

⚠️ 数字、货币、日期的格式受**当前区域设置（Culture）**影响。比如 `{price:C}` 的货币符号跟系统设置走（本机实测是 `$1,234.50`）。要固定格式就明确指定：

```csharp
Console.WriteLine(price.ToString("C", CultureInfo.GetCultureInfo("zh-CN")));
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{1234.5:N1}"));
```
```
¥1,234.50
1,234.5
```

写文件、拼 SQL、生成接口数据这类给机器读的字符串，用 `CultureInfo.InvariantCulture`。

## 10. 小结

1. 方法返回两三个值用值元组 `(int min, int max)`，用 `var (a, b) = ...` 解构；不要的值用 `_` 弃掉。
2. DTO、值对象用 `record`：自带值相等、`ToString`、`with`；注意 `with` 是浅拷贝、集合属性不参与"按内容比较"。
3. `switch` 表达式 + 模式匹配（类型、属性、关系、`and`/`or`/`not`、列表）替代长串 `if/else`；最后加 `_ =>`，别忽略 CS8509。
4. `string` 表示不为 `null`，`string?` 表示可能为 `null`；CS86xx 警告是在提前告诉你哪里会 NRE，用判空、`?.`、`??`、`required` 正确消除，`!` 慎用。
5. file-based app 用 `#:property`、`#:package` 指令代替 `.csproj`；用到反射/JSON 要 `#:property PublishAot=false`。
6. 隐式 using 不包含 `System.Text`、`System.Reflection`、`System.Text.Json`，看到 CS0246 先检查 `using`。
7. 正式项目用文件作用域 `namespace Xxx;`；字段用 `new()` 省略类型名。
8. JSON、SQL、正则用原始字符串 `"""`；插值格式 `{x:N2}`、`{x,8}`；给机器读的字符串用 `InvariantCulture`。

---

上一章：[异常与资源释放 🟢](08-exceptions-resources.md) · [目录](../README.md) · 下一章：[反射与特性 🟡](10-reflection-attributes.md)
