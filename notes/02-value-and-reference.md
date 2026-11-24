# 第 2 章 值类型与引用类型 🟢

> 本章练习：[08-struct-boxing.cs](../basics/08-struct-boxing.cs)、[12-clone-shallow-deep.cs](../basics/12-clone-shallow-deep.cs)、[13-reference-passing.cs](../basics/13-reference-passing.cs)、[14-method-parameters.cs](../basics/14-method-parameters.cs)

> 写代码时你会遇到这些问题：
> 把一个对象传进方法，方法里改了，外面有时候跟着变、有时候不变；复制了一份对象，改副本却把原对象也改了；`list[0].X = 5` 编译报错；Java 里 `==` 比较字符串是错的，C# 里却是对的；`ref`、`out`、`in` 三个关键字分不清。
> 这些问题的根源都是**值类型和引用类型**的区别。本章讲清楚：两种类型在内存里怎么放、赋值和传参时到底复制了什么、怎么正确地拷贝对象、方法参数的各种写法、`string` 为什么"像值类型"。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 两种类型 🟢

### 1.1 哪些是值类型，哪些是引用类型

| | 值类型 | 引用类型 |
|---|---|---|
| 包括 | 所有数值类型、`bool`、`char`、`decimal`、`DateTime`、**`struct`**、**`enum`**、元组 | **`class`**、`string`、数组、`interface`、委托、`record` |
| 基类 | `System.ValueType`（它再继承 `object`） | 直接继承 `object` |
| 变量里存的是 | **数据本身** | 对象的**地址**（引用） |
| 赋值 `b = a` | 复制一份数据，两者从此无关 | 复制地址，两者指向同一个对象 |
| 默认值 | 0、`false`，结构体是所有字段为 0 | `null` |
| Java 对应 | 基本类型 `int`、`double`…… | 所有对象 |

Java 里只有 8 种基本类型是值类型，你没法自己定义值类型。C# 可以用 `struct` 自定义值类型，这是最大的区别。

### 1.2 栈和堆的直觉

先建立一个够用的直觉（严格说法见下面的 ⚠️）：

```
int n = 10;
Point p = new Point(1, 2);     // struct
Line line = new Line();        // class

   栈（方法的局部变量）              堆（new 出来的对象）
  ┌──────────────────────┐
  │ n     │ 10           │
  │ p     │ X=1, Y=2     │       ┌──────────────┐
  │ line  │ 0x7f3a… ─────┼──────►│ Line 对象     │
  └──────────────────────┘       │ Length = 0   │
                                 └──────────────┘
```

- 值类型的局部变量，数据直接放在变量里（通常在栈上），方法结束就没了，不需要 GC。
- 引用类型的对象在**堆**上，变量里只存地址。对象没人引用之后，由 GC 回收。

⚠️ "值类型在栈上"不完全准确。准确的说法是：**值类型的数据存在它所在的地方**。作为局部变量时通常在栈上；作为 class 的字段时，跟着对象一起在堆上；放进数组里，在数组（堆）里。记住"赋值时复制的是数据还是地址"比记住"在栈还是堆"更有用。

## 2. struct vs class 🟢

### 2.1 赋值的区别

练习 08：

```csharp
var p1 = new Point(1, 1);
var p2 = p1;          // 复制了整个结构体
p2.X = 99;
Console.WriteLine($"p1={p1}, p2={p2}");

struct Point
{
    public int X;
    public int Y;
    public Point(int x, int y) { X = x; Y = y; }
    public override readonly string ToString() => $"Point(X={X}, Y={Y})";
}
```
```
p1=Point(X=1, Y=1), p2=Point(X=99, Y=1)
```

同样的代码，struct 和 class 对比：

```csharp
var ps = new PointS { X = 1 };
var ps2 = ps; ps2.X = 99;
var pc = new PointC { X = 1 };
var pc2 = pc; pc2.X = 99;
Console.WriteLine($"struct: ps.X={ps.X}, ps2.X={ps2.X}");
Console.WriteLine($"class : pc.X={pc.X}, pc2.X={pc2.X}");

var a = new PointS { X = 1, Y = 2 };
var b = new PointS { X = 1, Y = 2 };
Console.WriteLine($"struct Equals: {a.Equals(b)}");
var c1 = new PointC { X = 1, Y = 2 };
var c2 = new PointC { X = 1, Y = 2 };
Console.WriteLine($"class == : {c1 == c2}, Equals: {c1.Equals(c2)}");

struct PointS { public int X; public int Y; }
class PointC { public int X; public int Y; }
```
```
struct: ps.X=1, ps2.X=99
class : pc.X=99, pc2.X=99
struct Equals: True
class == : False, Equals: False
```

struct 的 `Equals` 默认**按字段逐个比较**；class 的 `==` 和 `Equals` 默认**比较地址**（和 Java 一样）。

### 2.2 对比表

| | `struct` | `class` |
|---|---|---|
| 类型 | 值类型 | 引用类型 |
| 赋值、传参 | 复制整个数据 | 复制地址 |
| 能否为 null | 不能（除非 `Point?`） | 能 |
| 继承 | 不能继承别的 struct/class，也不能被继承；**可以实现接口** | 单继承 + 多接口 |
| 默认 `Equals` | 按字段比较 | 按引用比较 |
| `==` | 默认没有，要自己重载 | 默认按引用比较 |
| 无参构造函数 | 永远可以 `new Point()` 或 `default`，所有字段为 0 | 只在没写其他构造函数时才有 |
| 适合 | 小（一般 ≤ 16 字节）、不可变、表示"一个值"：坐标、颜色、金额+币种 | 绝大多数情况 |

怎么选：**拿不准就用 class**。只有数据很小、语义上是"一个值"、而且不需要继承时，才考虑 struct。.NET 自带的 `DateTime`、`TimeSpan`、`Guid`、`decimal` 都是 struct。

### 2.3 struct 的坑 ⚠️

**坑 1：从集合里取出来的是副本**

```csharp
var list = new List<PointS> { new PointS { X = 1 } };
// list[0].X = 5;   // ❌ 编译错误 CS1612：不能修改返回值，因为它不是变量
var tmp = list[0]; tmp.X = 5;
Console.WriteLine($"list[0].X = {list[0].X}");
list[0] = tmp;      // 必须写回去
Console.WriteLine($"写回后 list[0].X = {list[0].X}");
```
```
list[0].X = 1
写回后 list[0].X = 5
```

`list[0]` 是方法调用（索引器），返回的是一个副本，改副本没有意义，所以编译器直接禁止。这也是为什么推荐 struct 设计成**不可变的**（字段只读，用 `readonly struct`）。

**坑 2：传给方法的是副本**（见第 4 节）。

**坑 3：装箱**（见第 3 节）。

## 3. 装箱和拆箱 🟡

### 3.1 是什么

值类型赋给 `object`（或它实现的接口）时，会在堆上新建一个对象，把值**复制**进去，这叫**装箱**；再转回来叫**拆箱**。练习 08：

```csharp
int num = 123;
object boxed = num;          // 装箱：值类型 → object，复制到堆上
int unboxed = (int)boxed;    // 拆箱：object → 值类型
Console.WriteLine($"boxed={boxed}, unboxed={unboxed}");
```
```
boxed=123, unboxed=123
```

装箱是复制，之后两者互不影响；拆箱必须拆成**原来的类型**：

```csharp
int num = 1;
object boxed = num;
num = 2;
Console.WriteLine($"num={num}, boxed={boxed}");

object o = 123;                           // 里面是 int
try { long l = (long)o; }                 // 直接拆成 long
catch (InvalidCastException e) { Console.WriteLine($"(long)object(int): {e.GetType().Name}"); }
long ok = (int)o;                         // 先拆成 int，再隐式转 long
Console.WriteLine($"(int)o 再转 long = {ok}");
```
```
num=2, boxed=1
(long)object(int): InvalidCastException
(int)o 再转 long = 123
```

Java 里的自动装箱 `Integer i = 1;` 是同一个概念。

### 3.2 性能影响

每次装箱都是一次堆分配，之后还要 GC 回收。在大循环里差别很明显。`ArrayList` 存的是 `object`，放 int 要装箱；`List<int>` 是泛型，直接存 int：

```csharp
const int N = 10_000_000;

var arrayList = new ArrayList();
for (int i = 0; i < N; i++) arrayList.Add(i);          // 每次 Add 都装箱
long sum1 = 0;
foreach (object x in arrayList) sum1 += (int)x;        // 每次都拆箱

var list = new List<int>();
for (int i = 0; i < N; i++) list.Add(i);               // 不装箱
long sum2 = 0;
foreach (int x in list) sum2 += x;
```

用 `Stopwatch` 计时、`GC.GetAllocatedBytesForCurrentThread()` 统计分配量，本机实测（`dotnet run` 默认配置，时间每次运行会有波动）：

```
ArrayList :   827 ms, 分配 484 MB
List<int> :    76 ms, 分配 128 MB
```

慢了 10 倍，多分配了 350 多 MB。`List<int>` 的 128 MB 是数组扩容产生的，提前指定容量 `new List<int>(N)` 还能更少。

怎么避免：

- 用泛型集合（`List<int>`、`Dictionary<string, int>`），**不要用 `ArrayList`、`Hashtable`**（.NET 1.0 时代的东西）。
- 方法参数需要"任意类型"时，用泛型 `T` 而不是 `object`（[第 5 章](05-generics.md)）。
- 值类型赋给接口变量也会装箱：`IComparable c = 5;`。

普通业务代码里偶尔的装箱（比如 `string.Format` 的参数）不用在意，**只在循环、高频路径上注意**。

## 4. 参数传递 🟢

### 4.1 默认都是"按值传递"

C# 和 Java 一样，默认传参时**复制变量里的内容**：

- 值类型：复制数据 → 方法里改不影响外面。
- 引用类型：复制**地址** → 方法里改对象的属性，外面能看到；但让参数指向新对象，外面看不到。

练习 08，struct 按值传递 vs `ref` 传递：

```csharp
Point point = new Point(100, 200);
DoSomethingByValue(point);
Console.Write("值传递之后：");  Write(point);
DoSomethingByRef(ref point);
Console.Write("ref 传递之后："); Write(point);

static void DoSomethingByValue(Point point) { point.X = 300; point.Y = 400; ... }
static void DoSomethingByRef(ref Point point) { point.X = 300; point.Y = 400; ... }
```
```
方法内（值传递）：Point(X=300, Y=400)
值传递之后：Point(X=100, Y=200)
方法内（ref）：Point(X=300, Y=400)
ref 传递之后：Point(X=300, Y=400)
```

### 4.2 引用类型的三种情况

练习 13，`Line` 是 class：

```csharp
Line line = new Line { Length = 50 };

Update1(line);   // 方法里 new 新对象
Console.WriteLine($"Update1 之后 Main: {line.Length}\n");

Update2(line);   // 方法里改属性
Console.WriteLine($"Update2 之后 Main: {line.Length}\n");

line.Length = 50;
Update3(ref line);   // ref + new 新对象
Console.WriteLine($"Update3 之后 Main: {line.Length}");

static void Update1(Line line) { line = new Line { Length = 100 }; }
static void Update2(Line line) { line.Length = 100; }
static void Update3(ref Line line) { line = new Line { Length = 200 }; }
```
```
Update1 Before: 50
Update1 After:  100
Update1 之后 Main: 50

Update2 Before: 50
Update2 After:  100
Update2 之后 Main: 100

Update3 Before: 50
Update3 After:  200
Update3 之后 Main: 200
```

| | 方法里改属性 `line.Length = …` | 方法里换对象 `line = new …` |
|---|---|---|
| 不加 `ref` | ✅ 外面跟着变（同一个对象） | ❌ 外面不变（只改了参数这个局部变量） |
| 加 `ref` | ✅ 外面跟着变 | ✅ 外面的变量也指向了新对象 |

图解 Update1：

```
调用前：  Main.line ──► [Length=50]
进入方法：Main.line ──► [Length=50] ◄── 参数 line（复制的地址）
new 之后：Main.line ──► [Length=50]
                         参数 line ──► [Length=100]   方法结束就没人用了
```

Java 里只有前两种情况，没有 `ref`，所以"在方法里替换调用方的对象"做不到。C# 加了 `ref` 就可以。

## 5. ref、out、in、params 🟢

### 5.1 对比

| 关键字 | 传进去前必须赋值？ | 方法里必须赋值？ | 方法里能改？ | 典型用途 |
|---|---|---|---|---|
| （无） | 是 | 否 | 改的是副本 | 绝大多数情况 |
| `ref` | **是** | 否 | 能，改的是调用方变量 | 交换两个变量、修改调用方的 struct |
| `out` | 否 | **是** | 能 | "多返回一个值"：`TryParse` |
| `in` | 是 | — | **不能**（只读） | 传大 struct 避免复制 |
| `params` | — | — | — | 可变数量参数 |

调用时 `ref` 和 `out` 必须写出来（`Swap(ref x, ref y)`），一眼能看出这个参数会被改；`in` 可以省略。

### 5.2 out

练习 14：

```csharp
int result = Square(-2, out string message);     // 调用时直接声明变量
Console.WriteLine($"area: {result}, message: {message}");
result = Square(2, out message);
Console.WriteLine($"area: {result}, message: {message}");

if (int.TryParse("123", out int n))              // 系统里最常见的 out
    Console.WriteLine($"TryParse: {n}");

static int Square(int length, out string message)
{
    if (length > 0)
    {
        message = "length is OK";
        return length * length;
    }
    message = "length should be greater than 0";   // 每条路径都必须给 out 赋值
    return length;
}
```
```
area: -2, message: length should be greater than 0
area: 4, message: length is OK
TryParse: 123
```

不需要某个 out 的值时用弃元 `_`：`if (int.TryParse("42", out _)) ...`。

需要返回多个值时，现在更推荐用**元组**：`(int area, string message) Square(int length)`（[第 9 章](09-modern-syntax.md)）。`out` 主要用在 `TryXxx` 这种"返回成功与否 + 结果"的模式。

### 5.3 ref 和 in

```csharp
int x = 1, y = 2;
Swap(ref x, ref y);
Console.WriteLine($"x={x}, y={y}");

var big = new BigStruct { A = 1 };
Print(in big);
Print(big);              // in 调用时可以省略

int[] scores = [60, 70, 80];
ref int lowest = ref FindMin(scores);   // ref 返回：拿到数组元素本身
lowest = 100;
Console.WriteLine(string.Join(",", scores));

static void Swap(ref int a, ref int b) { (a, b) = (b, a); }
static void Print(in BigStruct s)
{
    // s.A = 2;          // ❌ 编译错误 CS8332：in 参数是只读的
    Console.WriteLine($"in: A={s.A}");
}
static ref int FindMin(int[] arr)
{
    int idx = 0;
    for (int i = 1; i < arr.Length; i++) if (arr[i] < arr[idx]) idx = i;
    return ref arr[idx];
}
struct BigStruct { public long A, B, C, D; }
```
```
x=2, y=1
in: A=1
in: A=1
100,70,80
```

`in` 和 `ref` 返回是性能优化手段，业务代码里很少用，认识就行。

### 5.4 params 可变参数

相当于 Java 的 `String... args`。练习 14：

```csharp
Console.WriteLine(Union(null));             // 传 null
Console.WriteLine(Union());                 // 不传 → 空数组
Console.WriteLine(Union("Tom", "Jerry"));   // 编译器自动打包成数组
Console.WriteLine(Union(["Tom", "Jerry"])); // 也可以直接传数组

// params 必须是最后一个参数
static string Union(params string[]? array)
{
    if (array is null) return "array is null";
    if (array.Length == 0) return "array.length is 0";
    return string.Join(", ", array);
}
```
```
array is null
array.length is 0
Tom, Jerry
Tom, Jerry
```

## 6. 可选参数与命名参数 🟢

Java 只能靠写一堆重载来模拟默认参数，C# 直接支持。练习 14：

```csharp
Console.WriteLine(GetPerimeter(2));              // pi 用默认值
Console.WriteLine(GetPerimeter(2, Math.PI));

Print();
Print("Owen", "Yin", 38);
Print(lastName: "Yin", age: 38);   // 命名参数：跳过 firstName
Print(age: 20, firstName: "Tom");  // 顺序随意

// 可选参数必须放在必选参数后面，默认值必须是编译时常量
static double GetPerimeter(double diameter, double pi = 3.1415926) => pi * diameter;

static void Print(string firstName = "", string lastName = "", int age = 18)
    => Console.WriteLine($"name: {lastName} {firstName}, age: {age}");
```
```
6.2831852
6.283185307179586
name:  , age: 18
name: Yin Owen, age: 38
name: Yin , age: 38
name:  Tom, age: 20
```

- 参数多、又都是 `bool` 或数字时，用命名参数能让调用处更好读：`Send(msg, retry: true, timeout: 30)`。
- ⚠️ 默认值和 `const` 一样是**编译进调用方**的。公开库里改了默认值，调用方要重新编译才生效。
- 默认值只能是常量，所以 `DateTime now = DateTime.Now` 不行。要"默认当前时间"，写成 `DateTime? now = null`，方法里 `now ??= DateTime.Now`。

## 7. 深拷贝与浅拷贝 🟡

### 7.1 MemberwiseClone 是浅拷贝

`object` 有一个 `protected` 方法 `MemberwiseClone()`，按字段逐个复制：值类型字段复制数据，引用类型字段**只复制地址**。练习 12：

```csharp
class Movie : ICloneable
{
    public int Id { get; set; }
    public string Film { get; set; } = string.Empty;
    public string[] Audiences { get; set; } = ["Alice", "Bob", "Chris"];

    public object Clone() => MemberwiseClone();   // 浅拷贝
    public void Show() => Console.WriteLine($"id={Id}, film={Film}, audiences={string.Join(" ", Audiences)}");
}

Movie movie = new Movie { Id = 1, Film = "Titanic" };
Movie shallow = (Movie)movie.Clone();
Update(shallow);       // 改 Id、Film，以及 Audiences[0..2]
Console.Write("副本  "); shallow.Show();
Console.Write("原对象"); movie.Show();
```
```
原对象：
id=1, film=Titanic, audiences=Alice Bob Chris

浅拷贝并修改副本后：
副本  id=2, film=xxxx, audiences=Dick Emma Frank
原对象id=1, film=Titanic, audiences=Dick Emma Frank
```

`Id`（值类型）和 `Film`（string，赋值换的是新字符串）互不影响，但 `Audiences` 数组是两个对象**共用**的，副本改了原对象也变了。

```
movie   ──► [Id=1, Film ─► "Titanic", Audiences ─┐]
                                                 ├──► ["Dick","Emma","Frank"]
shallow ──► [Id=2, Film ─► "xxxx",    Audiences ─┘]
```

### 7.2 深拷贝的几种做法

| 做法 | 优点 | 缺点 |
|---|---|---|
| 手写：先 `MemberwiseClone`，再把引用字段单独复制 | 最快、可控 | 字段多了容易漏，加字段时要记得改 |
| 序列化再反序列化（`System.Text.Json`） | 一行代码、不会漏 | 慢；只复制公开属性；类型要能序列化 |
| `record` + `with` | 语法简洁 | 也是浅拷贝（[第 9 章](09-modern-syntax.md)） |

手写（练习 12 注释里的写法）：

```csharp
public object Clone()
{
    var copy = (Movie)MemberwiseClone();
    copy.Audiences = (string[])Audiences.Clone();   // 数组单独复制一份
    return copy;
}
```

序列化（练习 12）：

```csharp
static T CloneDeep<T>(T obj)
{
    string json = JsonSerializer.Serialize(obj);
    return JsonSerializer.Deserialize<T>(json)!;
}

movie = new Movie { Id = 1, Film = "Titanic" };
Movie deep = CloneDeep(movie);
Update(deep);
```
```
深拷贝并修改副本后：
副本  id=2, film=xxxx, audiences=Dick Emma Frank
原对象id=1, film=Titanic, audiences=Alice Bob Chris
```

- 单文件程序默认开 AOT，反射式 JSON 不可用，所以练习 12 第一行是 `#:property PublishAot=false`。
- 老教程里的 `BinaryFormatter` 深拷贝已经在 .NET 9 **移除**了（有安全漏洞），不要再用。
- ⚠️ `ICloneable` 接口没有说明是深拷贝还是浅拷贝，调用方无从得知，微软的设计指南也不推荐在公开 API 里实现它。自己的代码里，起个明确的名字更好：`ShallowCopy()` / `DeepCopy()`。

## 8. string：不可变的引用类型 🟢

### 8.1 == 比较的是内容

`string` 是引用类型，但 C# 给它**重载了 `==`**，比较的是内容。这是从 Java 过来最容易反应不过来的地方：

```csharp
string s1 = "hello";
string s2 = "hel" + "lo";                            // 编译期常量折叠，和 s1 是同一个对象
string s3 = new string(['h', 'e', 'l', 'l', 'o']);   // 运行时新建的对象
Console.WriteLine($"s1 == s3: {s1 == s3}");
Console.WriteLine($"s1.Equals(s3): {s1.Equals(s3)}");
Console.WriteLine($"ReferenceEquals(s1, s2): {ReferenceEquals(s1, s2)}");
Console.WriteLine($"ReferenceEquals(s1, s3): {ReferenceEquals(s1, s3)}");

object o1 = s1, o3 = s3;
Console.WriteLine($"(object)s1 == (object)s3: {o1 == o3}");   // 编译器会警告 CS0252/CS0253
Console.WriteLine($"object.Equals(o1, o3): {Equals(o1, o3)}");

Console.WriteLine($"忽略大小写: {string.Equals("Hello", "HELLO", StringComparison.OrdinalIgnoreCase)}");
string? n = null;
Console.WriteLine($"null == \"a\": {n == "a"}");
```
```
s1 == s3: True
s1.Equals(s3): True
ReferenceEquals(s1, s2): True
ReferenceEquals(s1, s3): False
(object)s1 == (object)s3: False
object.Equals(o1, o3): True
忽略大小写: True
null == "a": False
```

| 比较方式 | Java | C# |
|---|---|---|
| 比较内容 | `a.equals(b)` | **`a == b`**（推荐）或 `a.Equals(b)` |
| 比较是不是同一个对象 | `a == b` | `ReferenceEquals(a, b)` |
| 忽略大小写 | `a.equalsIgnoreCase(b)` | `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` |
| a 可能是 null | `a.equals(b)` 会 NPE | `a == b` 不会抛异常 |

⚠️ `==` 是编译时按**变量类型**选的。两边都声明成 `object` 时，用的是 `object` 的 `==`，比较的是地址，结果是 `False`。编译器会给出警告 CS0252/CS0253，看到了要改。

### 8.2 不可变

`string` 一旦创建就不能改。所有"修改"方法（`ToUpper`、`Replace`、`Trim`、`+=`）都是**返回新字符串**：

```csharp
string s = "abc";
string upper = s.ToUpper();
s.Replace("a", "x");          // ⚠️ 返回值被扔掉了，s 本身不变
Console.WriteLine($"s={s}, upper={upper}");

string t = s;
s += "d";                     // s 指向了新字符串，t 还指向旧的
Console.WriteLine($"s={s}, t={t}");

Change(s);
Console.WriteLine($"Change 之后 s={s}");
static void Change(string str) { str += "!!!"; }   // 只改了参数这个局部变量
```
```
s=abc, upper=ABC
s=abcd, t=abc
Change 之后 s=abcd
```

所以 string 用起来"像值类型"：赋值、传参之后，谁也改不了谁。原因不是它是值类型，而是它**不可变** + **`==` 比内容**。

`s.Replace(...)` 不接返回值是常见 bug，正确写法是 `s = s.Replace("a", "x");`。

## 9. 小结

1. 值类型（数值、`bool`、`struct`、`enum`）变量里存数据，赋值复制数据；引用类型（`class`、`string`、数组）变量里存地址，赋值复制地址。
2. 拿不准就用 `class`；`struct` 只用于小而不可变的"值"，注意从集合里取出的是副本。
3. 值类型转 `object` 或接口会装箱（堆分配 + 复制），大循环里差 10 倍；用泛型集合，不用 `ArrayList`。
4. 默认传参都是按值传递：引用类型能改对象内容，不能换掉调用方的对象；要换就用 `ref`。
5. `out` 用于 `TryXxx` 模式，`in` 是只读引用，`params` 是可变参数；可选参数和命名参数能代替一堆重载。
6. `MemberwiseClone` 是浅拷贝，引用字段是共享的；深拷贝手写或用 JSON 序列化，不要用 `BinaryFormatter`。
7. C# 的 `string` 用 `==` 比较内容（和 Java 相反）；它不可变，`Replace`、`ToUpper` 都要接返回值。

---

上一章：[类型与基础语法 🟢](01-types-and-syntax.md) · [目录](../README.md) · 下一章：[类与对象 🟢](03-classes.md)
