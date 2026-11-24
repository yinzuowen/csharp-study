# 第 1 章 类型与基础语法 🟢

> 本章练习：[01-object-var-dynamic.cs](../basics/01-object-var-dynamic.cs)、[02-type-conversion.cs](../basics/02-type-conversion.cs)、[03-literals.cs](../basics/03-literals.cs)、[04-const-readonly-static.cs](../basics/04-const-readonly-static.cs)、[05-ref-nullable-operators.cs](../basics/05-ref-nullable-operators.cs)、[07-enum.cs](../basics/07-enum.cs)

> 从 Java 转过来写 C#，头几天你会遇到这些问题：
> 金额用 `double` 算出来差了一点点、`(int)3.99` 和 `Convert.ToInt32(3.99)` 结果不一样、`int.MaxValue + 1` 不报错却变成负数、`0123` 不是八进制、`var` 到底是不是弱类型、`int?` 和 `??` 是什么、`[Flags]` 枚举打印出来是 `Read, Write`。
> 本章讲清楚：C# 的内置类型和 Java 的对应关系、字符串怎么拼、类型怎么转、常量怎么定义、可空值类型和几个"问号运算符"、枚举怎么用。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 内置类型

### 1.1 和 Java 对照 🟢

C# 的关键字类型其实是 .NET 类型的别名：`int` 就是 `System.Int32`，`string` 就是 `System.String`。两种写法完全一样，习惯上写关键字。

| C# 关键字 | .NET 类型 | 大小 | Java 对应 | 说明 |
|---|---|---|---|---|
| `bool` | `System.Boolean` | 1 字节 | `boolean` | 只有 `true`/`false`，**不能和 int 互转**（`if (1)` 编译错误） |
| `byte` | `System.Byte` | 1 字节 | — | **无符号** 0 ~ 255（Java 的 `byte` 是有符号的） |
| `sbyte` | `System.SByte` | 1 字节 | `byte` | 有符号 -128 ~ 127 |
| `short` / `ushort` | `Int16` / `UInt16` | 2 字节 | `short` / — | |
| `int` / `uint` | `Int32` / `UInt32` | 4 字节 | `int` / — | |
| `long` / `ulong` | `Int64` / `UInt64` | 8 字节 | `long` / — | |
| `char` | `System.Char` | 2 字节 | `char` | UTF-16 代码单元，和 Java 一样 |
| `float` | `System.Single` | 4 字节 | `float` | |
| `double` | `System.Double` | 8 字节 | `double` | 小数字面量默认是 `double` |
| `decimal` | `System.Decimal` | 16 字节 | `BigDecimal`（近似） | 十进制小数，**金额用它** |
| `string` | `System.String` | 引用类型 | `String` | `==` 比较的是内容（见[第 2 章](02-value-and-reference.md)） |
| `object` | `System.Object` | 引用类型 | `Object` | 所有类型的基类，**包括 int** |

和 Java 最大的几个区别：

- C# 有**无符号整数**（`byte`、`ushort`、`uint`、`ulong`）。注意 C# 的 `byte` 是无符号的，和 Java 正好相反。
- C# 有 `decimal`，是语言内置类型，可以直接 `+ - * /`，不用像 Java 的 `BigDecimal` 那样调 `add()`。
- `int`、`double` 这些在 C# 里也是"对象"（结构体），可以直接调方法：`123.ToString()`、`int.Parse("1")`。没有 Java 那种 `int` / `Integer` 两套类型。

```csharp
Console.WriteLine($"int    {sizeof(int)} 字节  {int.MinValue} ~ {int.MaxValue}");
Console.WriteLine($"uint   {sizeof(uint)} 字节  {uint.MinValue} ~ {uint.MaxValue}");
Console.WriteLine($"byte   {sizeof(byte)} 字节  {byte.MinValue} ~ {byte.MaxValue}");
Console.WriteLine($"sbyte  {sizeof(sbyte)} 字节  {sbyte.MinValue} ~ {sbyte.MaxValue}");
Console.WriteLine($"decimal {sizeof(decimal)} 字节");
Console.WriteLine($"int 是 {typeof(int).FullName}，string 是 {typeof(string).FullName}");
```
```
int    4 字节  -2147483648 ~ 2147483647
uint   4 字节  0 ~ 4294967295
byte   1 字节  0 ~ 255
sbyte  1 字节  -128 ~ 127
decimal 16 字节
int 是 System.Int32，string 是 System.String
```

### 1.2 金额用 decimal ⚠️

`double` 是二进制浮点数，0.1 这种十进制小数存不精确：

```csharp
double d1 = 0.1 + 0.2;
decimal m1 = 0.1m + 0.2m;
Console.WriteLine($"double:  0.1 + 0.2 = {d1:R}, == 0.3 ? {d1 == 0.3}");
Console.WriteLine($"decimal: 0.1 + 0.2 = {m1}, == 0.3 ? {m1 == 0.3m}");
```
```
double:  0.1 + 0.2 = 0.30000000000000004, == 0.3 ? False
decimal: 0.1 + 0.2 = 0.3, == 0.3 ? True
```

| 场景 | 用什么 |
|---|---|
| 金额、价格、汇率、要和账对上的数 | `decimal`（字面量加 `m`：`19.99m`） |
| 科学计算、坐标、统计、性能敏感 | `double` |
| 一般不用 | `float`（精度只有 7 位左右） |

数据库里的 `decimal(18,2)` 列，在 C# 里也对应 `decimal`。

## 2. 字符串拼接与插值 🟢

### 2.1 几种拼接方式

练习 01 里的写法：

```csharp
string firstName = "Owen", lastName = "Yin";
Console.WriteLine(firstName + lastName);                 // + 拼接

char[] chars = ['O', 'w', 'e', 'n'];                     // char 用单引号
Console.WriteLine(new string(chars));                    // char[] → string

string[] parts = ["Owen", "Yin"];
Console.WriteLine(string.Join(" ", parts));              // 用分隔符连接
```
```
OwenYin
Owen
Owen Yin
```

`['O', 'w', 'e', 'n']` 是 C# 12 的**集合表达式**，相当于 `new char[] { 'O', 'w', 'e', 'n' }`。

### 2.2 字符串插值 `$""`（最常用）

`$` 开头的字符串里，`{}` 里面可以写任意表达式，相当于 Java 的 `String.format` 但更直观：

```csharp
string name = "Owen";
int age = 38;
decimal price = 1234.5m;
DateTime t = new DateTime(2026, 11, 21, 9, 5, 3);

Console.WriteLine("Name: " + name + ", Age: " + age);               // +
Console.WriteLine(string.Format("Name: {0}, Age: {1}", name, age)); // 格式化，像 Java 的 String.format
Console.WriteLine($"Name: {name}, Age: {age}");                     // 插值
Console.WriteLine($"明年 {age + 1} 岁，{(age >= 18 ? "成年" : "未成年")}");
Console.WriteLine($"价格 {price:F2} | {price:N2} | {price,10:F1}| {age:D5} | {0.256:P1}");
Console.WriteLine($"时间 {t:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"花括号要写两次：{{ {name} }}");
```
```
Name: Owen, Age: 38
Name: Owen, Age: 38
Name: Owen, Age: 38
明年 39 岁，成年
价格 1234.50 | 1,234.50 |     1234.5| 00038 | 25.6%
时间 2026-11-21 09:05:03
花括号要写两次：{ Owen }
```

格式速查（`{值:格式}`，`{值,宽度}` 是对齐，正数右对齐、负数左对齐）：

| 格式 | 含义 | 例子 |
|---|---|---|
| `F2` | 固定 2 位小数 | `1234.50` |
| `N2` | 千分位 + 2 位小数 | `1,234.50` |
| `D5` | 整数补零到 5 位 | `00038` |
| `P1` | 百分比，1 位小数 | `25.6%` |
| `X` | 十六进制 | `FF` |
| `yyyy-MM-dd HH:mm:ss` | 日期时间（`HH` 24 小时制，`hh` 12 小时制） | `2026-11-21 09:05:03` |

⚠️ 插值里用三元运算符要加括号：`{(age >= 18 ? "成年" : "未成年")}`，否则 `:` 会被当成格式分隔符。

### 2.3 逐字字符串和原始字符串

```csharp
string path = $@"C:\Users\{name}";       // @ 逐字：\ 不转义；可以和 $ 一起用
Console.WriteLine(path);

string json = $$"""
    { "name": "{{name}}", "age": {{age}} }
    """;                                  // 原始字符串：引号不用转义；$$ 表示 {{ }} 才是插值
Console.WriteLine(json);
```
```
C:\Users\Owen
{ "name": "Owen", "age": 38 }
```

### 2.4 循环里拼接用 StringBuilder

和 Java 一样，`string` 是不可变的，循环里 `+=` 每次都会生成新字符串。拼很多次时用 `StringBuilder`：

```csharp
var sb = new System.Text.StringBuilder();
for (int i = 0; i < 3; i++) sb.Append(i).Append(',');
Console.WriteLine(sb.ToString());
```
```
0,1,2,
```

## 3. object、var、dynamic 🟢

三个都能"装各种东西"，但完全不是一回事：

| | `object` | `var` | `dynamic` |
|---|---|---|---|
| 是什么 | 所有类型的基类 | 编译器**推断**类型的语法糖 | 关闭编译期检查 |
| 类型何时确定 | 变量类型就是 `object` | **编译时**，之后固定 | **运行时** |
| 能调实际类型的方法吗 | 不能，要先强转 | 能（类型就是推断出来的那个） | 能，写错了运行时才报错 |
| 能换成别的类型吗 | 能（都是 object） | 不能 | 能 |
| Java 对应 | `Object` | Java 10 的 `var` | 没有 |
| 日常用 | 少 | **很多** | 很少（COM 互操作、动态 JSON） |

练习 01：

```csharp
object o = 99;                      // 变量类型是 object，可以装任何值
Console.WriteLine($"o.GetType(): {o.GetType()}");   // 实际类型 System.Int32
o = "test";
Console.WriteLine($"o.GetType(): {o.GetType()}");   // System.String

dynamic d = 99;                     // 运行时才确定类型，编译器不检查
Console.WriteLine($"d.GetType(): {d.GetType()}");
d = "test";
Console.WriteLine($"d.Length: {d.Length}");          // 运行时才去找 Length

var v = 1;                          // 编译时推断为 int，之后类型固定
Console.WriteLine($"v.GetType(): {v.GetType()}");
// v = "test";                      // ❌ 编译错误：v 已经是 int
```
```
o.GetType(): System.Int32
o.GetType(): System.String
d.GetType(): System.Int32
d.Length: 4
v.GetType(): System.Int32
```

`dynamic` 写错了，编译能过，运行才炸：

```csharp
dynamic d = 99;
Console.WriteLine(d.Length);   // 编译通过
```
```
Microsoft.CSharp.RuntimeBinder.RuntimeBinderException: 'int' does not contain a definition for 'Length'
```

- **`var` 不是弱类型**，它和写出完整类型名完全等价，只是少打字。右边类型一眼能看出来时用 `var`：`var list = new List<string>();`。
- 声明变量时没有初值不能用 `var`（`var x;` 编译错误）。
- `dynamic` 用到反射，单文件程序要在第一行加 `#:property PublishAot=false`。

`object` 的几个常用方法（练习 01）：

```csharp
object o1 = new object();
object o2 = new object();
object o3 = o1;
Console.WriteLine($"o1 == o2: {Equals(o1, o2)}");        // False，不同对象
Console.WriteLine($"o1 == o3: {Equals(o1, o3)}");        // True，同一个对象
Console.WriteLine($"o1.GetHashCode(): {o1.GetHashCode()}");
Console.WriteLine($"o1.GetType(): {o1.GetType()}");
```
```
o1 == o2: False
o1 == o3: True
o1.GetHashCode(): 54267293
o1.GetType(): System.Object
```

（哈希码每次运行都不一样。）`GetType()` 相当于 Java 的 `getClass()`，`typeof(int)` 相当于 `int.class`。

## 4. 类型转换 🟢

### 4.1 隐式和显式

| 方式 | 写法 | 什么时候用 | 失败时 |
|---|---|---|---|
| 隐式转换 | `long l = i;` | 小范围 → 大范围，不会丢数据 | 不会失败 |
| 显式转换（强转） | `(int)3.99` | 大 → 小、浮点 → 整数 | 截断小数；溢出时**默认不报错** |
| `Convert.ToXxx` | `Convert.ToInt32(3.99)` | 各种类型互转，含 `string` | 四舍五入（银行家舍入）；格式错抛异常 |
| `Xxx.Parse` | `int.Parse("123")` | 字符串 → 数字 | 抛 `FormatException` / `OverflowException` |
| `Xxx.TryParse` | `int.TryParse(s, out int n)` | 字符串 → 数字，**用户输入首选** | 返回 `false`，不抛异常 |
| `ToString()` | `123.ToString()` | 任何类型 → 字符串 | — |

```csharp
int i = 100;
long l = i;          // 隐式：int → long
double d = i;        // 隐式：int → double

double pi = 3.99;
int n1 = (int)pi;                 // 显式：直接截断小数
int n2 = Convert.ToInt32(pi);     // Convert：四舍五入
Console.WriteLine($"(int)3.99 = {n1}, Convert.ToInt32(3.99) = {n2}");
Console.WriteLine($"Convert.ToInt32(2.5) = {Convert.ToInt32(2.5)}, Convert.ToInt32(3.5) = {Convert.ToInt32(3.5)}");
Console.WriteLine($"Math.Round(2.5) = {Math.Round(2.5)}, Math.Round(2.5, MidpointRounding.AwayFromZero) = {Math.Round(2.5, MidpointRounding.AwayFromZero)}");
```
```
(int)3.99 = 3, Convert.ToInt32(3.99) = 4
Convert.ToInt32(2.5) = 2, Convert.ToInt32(3.5) = 4
Math.Round(2.5) = 2, Math.Round(2.5, MidpointRounding.AwayFromZero) = 3
```

⚠️ `Convert.ToInt32` 和 `Math.Round` 默认是**银行家舍入**（四舍六入五取偶）：2.5 → 2，3.5 → 4。要"四舍五入"必须写 `MidpointRounding.AwayFromZero`。算钱时尤其注意。

### 4.2 Parse 和 TryParse

练习 02：

```csharp
string text = "12.34";
Console.WriteLine($"float.Parse  = {float.Parse(text)}");
Console.WriteLine($"short.Parse  = {short.Parse("32767")}");   // 超过 32767 抛 OverflowException
Console.WriteLine($"byte.Parse   = {byte.Parse("129")}");      // 超过 255 抛 OverflowException
Console.WriteLine($"int.Parse    = {int.Parse("1234")}");      // int.Parse("12.34") 抛 FormatException

string words = "hello.world";
if (float.TryParse(words, out float money))
    Console.WriteLine($"money = {money}");
else
    Console.WriteLine($"cannot convert {words} to float");

string source = "256";
if (byte.TryParse(source, out byte b))
    Console.WriteLine($"b = {b}");
else
    Console.WriteLine($"cannot convert {source} to byte");     // 256 超出 byte 范围
```
```
float.Parse  = 12.34
short.Parse  = 32767
byte.Parse   = 129
int.Parse    = 1234
cannot convert hello.world to float
cannot convert 256 to byte
```

几种失败情况对比（自己写的示例）：

```csharp
int.Parse("12.34");                  // FormatException
int.Parse(null!);                    // ArgumentNullException
Convert.ToInt32((string?)null);      // 0，不抛异常
int.TryParse("abc", out int r);      // 返回 False，r = 0
```
```
int.Parse("12.34"): FormatException
int.Parse(null): ArgumentNullException
Convert.ToInt32(null) = 0
int.TryParse("abc") = False, r = 0
```

`out int r` 是在调用处直接声明变量，`TryParse` 失败时会把它设成默认值 0。`out` 的细节见[第 2 章](02-value-and-reference.md)。

### 4.3 溢出与 checked ⚠️

和 Java 一样，整数溢出**默认不报错**，直接回绕：

```csharp
int big = 300;
byte b1 = (byte)big;              // 只保留低 8 位
Console.WriteLine($"(byte)300 = {b1}");

int max = int.MaxValue;
int overflow = max + 1;
Console.WriteLine($"int.MaxValue + 1 = {overflow}");

try
{
    int x = checked(max + 1);     // checked 表达式：溢出抛异常
}
catch (OverflowException e)
{
    Console.WriteLine($"checked: {e.GetType().Name}: {e.Message}");
}

try
{
    checked { byte b2 = (byte)big; }   // checked 块：块里所有运算都检查
}
catch (OverflowException e)
{
    Console.WriteLine($"checked 块: {e.GetType().Name}");
}
```
```
(byte)300 = 44
int.MaxValue + 1 = -2147483648
checked: OverflowException: Arithmetic operation resulted in an overflow.
checked 块: OverflowException
```

这是 C# 比 Java 多的能力：计数、金额累加这种绝对不能静默溢出的地方，包一层 `checked`。也可以在项目文件里加 `<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>` 全局打开。

### 4.4 BitConverter ⚪

把一串字节按某种类型重新解读，做二进制协议时才用到。练习 02：

```csharp
byte[] bytes = [0, 1, 2, 3, 4, 5];
Console.WriteLine($"bytes = {BitConverter.ToString(bytes)}");
Console.WriteLine($"IsLittleEndian = {BitConverter.IsLittleEndian}");    // 小端序：低位字节在前
Console.WriteLine($"ToInt16(3)   = {BitConverter.ToInt16(bytes, 3)}");    // 03 04 → 0x0403
Console.WriteLine($"ToInt32(0)   = {BitConverter.ToInt32(bytes, 0)}");    // 00 01 02 03 → 0x03020100
```
```
bytes = 00-01-02-03-04-05
IsLittleEndian = True
ToInt16(3)   = 1027
ToInt32(0)   = 50462976
```

## 5. 字面量 🟢

练习 03（`Show` 打印值和实际类型）：

```csharp
Show(123);        // int
Show(123u);       // uint：后缀 u
Show(0x1234L);    // long：后缀 L（小写 l 容易和 1 混淆，推荐大写）
Show(0x1234ul);   // ulong
Show(0123);       // 123！C# 没有八进制字面量，前导 0 被忽略
Show(0b0111_1011);// 二进制 → 123，下划线只是分隔符
Show(Convert.ToInt32("123", 8));  // 真要八进制：用 Convert → 83

Show(3.1415926);    // double（默认）
Show(31415926E-7);  // 科学计数法
Show(.5);
Show(1.5f);         // float：后缀 f
Show(1.5m);         // decimal：后缀 m

Show('\u0061');     // Unicode 编号 → 'a'
Show("C:\\");       // 普通字符串，\ 要转义
Show(@"C:\");       // 逐字字符串
Show("""
    原始字符串 "引号" 不用转义
    """);

static void Show(object value) => Console.WriteLine($"{value}  ({value.GetType().Name})");
```
```
123  (Int32)
123  (UInt32)
4660  (Int64)
4660  (UInt64)
123  (Int32)
123  (Int32)
83  (Int32)
3.1415926  (Double)
3.1415926  (Double)
0.5  (Double)
1.5  (Single)
1.5  (Decimal)
a  (Char)
C:\  (String)
C:\  (String)
原始字符串 "引号" 不用转义  (String)
```

| 后缀 / 前缀 | 类型 | 例子 |
|---|---|---|
| 无（整数） | `int`（放不下时自动变 `long`） | `123` |
| `u` / `L` / `ul` | `uint` / `long` / `ulong` | `123u`、`10L` |
| 无（小数） | `double` | `3.14` |
| `f` / `m` | `float` / `decimal` | `1.5f`、`19.99m` |
| `0x` / `0b` | 十六进制 / 二进制 | `0xFF`、`0b1010` |
| `_` | 数字分隔符，只为好读 | `1_000_000` |

⚠️ Java 里 `0123` 是八进制（= 83），C# 里就是 123。

⚠️ `decimal price = 19.99;` 编译错误：`19.99` 是 `double`，不能隐式转 `decimal`，要写 `19.99m`。

## 6. const、readonly、static 🟢

练习 04：

```csharp
class MyMath
{
    // const 只能是内置类型或 string
    public const double PI = 3.1415926;
    public const string Hello = "hello";

    // static readonly：只能在声明时或静态构造函数里赋值
    public static readonly double PI2;
    static MyMath() { PI2 = 3.1415926; }

    // 实例 readonly：只能在声明时或普通构造函数里赋值
    public readonly double E = 0;
    public MyMath(double e) { E = e; }

    // public void Change() { E = 1; }   // ❌ 构造函数以外不能改
}

class Counter
{
    public static int Count = 0;          // 普通 static 字段：属于类，可以改
}
```

```csharp
Console.WriteLine($"MyMath.PI  = {MyMath.PI}");           // const：自动是 static
Console.WriteLine($"MyMath.PI2 = {MyMath.PI2}");
Console.WriteLine($"System.Math.PI = {Math.PI}");
var m = new MyMath(2.718);
Console.WriteLine($"m.E = {m.E}");
Counter.Count = 10;
Console.WriteLine($"Counter.Count = {Counter.Count}");
```
```
MyMath.PI  = 3.1415926
MyMath.PI2 = 3.1415926
System.Math.PI = 3.141592653589793
m.E = 2.718
Counter.Count = 10
```

| | `const` | `static readonly` | `readonly`（实例） | `static` |
|---|---|---|---|---|
| 值什么时候定 | **编译时** | 运行时，类第一次使用时 | 运行时，`new` 时 | 随时可改 |
| 在哪里赋值 | 只能在声明处 | 声明处或静态构造函数 | 声明处或构造函数 | 任何地方 |
| 能用的类型 | 内置数值、`bool`、`char`、`string`、enum | 任意 | 任意 | 任意 |
| 属于 | 类（隐式 static） | 类 | 对象 | 类 |
| Java 对应 | `static final`（编译期常量） | `static final` | `final` 字段 | `static` |

怎么选：

- 真正永远不变的数学常量、魔法字符串：`const`。
- 运行时才能算出来的、或者是对象（`DateTime`、`List`、配置）：`static readonly`。
- ⚠️ `public const` 的值会被**编译进调用方的程序集**。如果 A 库改了常量值，引用它的 B 没重新编译，B 里还是旧值。对外公开的"常量"如果将来可能改，用 `static readonly`。
- ⚠️ `readonly` 只保证**引用**不变：`readonly List<int> _list` 不能再指向别的 List，但可以 `_list.Add(1)`。这一点和 Java 的 `final` 一样。

`@` 可以把关键字当变量名（练习 04）：`float @class = 0;`。一般只在对接外部数据（比如 JSON 字段叫 `class`）时用。

## 7. ref 局部变量、可空值类型和问号运算符

### 7.1 ref 局部变量 🟡

`ref` 局部变量是另一个变量的**别名**，两者是同一块存储。练习 05：

```csharp
int a = 100;
int b = a;          // b 是复制
ref int c = ref a;  // c 是 a 的别名
Console.WriteLine($"a={a}, b={b}, c={c}");
a = 200; Console.WriteLine($"a=200 后: a={a}, b={b}, c={c}");
b = 300; Console.WriteLine($"b=300 后: a={a}, b={b}, c={c}");
c = 400; Console.WriteLine($"c=400 后: a={a}, b={b}, c={c}");
```
```
a=100, b=100, c=100
a=200 后: a=200, b=100, c=200
b=300 后: a=200, b=300, c=200
c=400 后: a=400, b=300, c=400
```

也可以指向数组元素，直接改数组里的值：

```csharp
int[] nums = [1, 2, 3];
ref int first = ref nums[0];
first = 100;
Console.WriteLine($"nums = {string.Join(",", nums)}");
```
```
nums = 100,2,3
```

日常业务代码很少用，主要用在高性能代码里避免复制大结构体。更常见的是 `ref` **参数**，见[第 2 章](02-value-and-reference.md)。

### 7.2 可空值类型 `int?` 🟢

值类型（`int`、`bool`、`DateTime`、`decimal`……）默认不能是 `null`。需要表示"没有值"时（比如数据库里可空的列），用 `int?`，它是 `Nullable<int>` 的简写。相当于 Java 里用 `Integer` 来表达 null。

```csharp
int? a = null;
Console.WriteLine($"a.HasValue = {a.HasValue}, a ?? -1 = {a ?? -1}, GetValueOrDefault = {a.GetValueOrDefault()}");
try { int v = a.Value; }
catch (InvalidOperationException e) { Console.WriteLine($"a.Value: {e.GetType().Name}"); }

a = 5;
Console.WriteLine($"a.HasValue = {a.HasValue}, a.Value = {a.Value}");

int? sum = a + null;      // 任何一边是 null，结果就是 null
Console.WriteLine($"5 + null = {(sum is null ? "null" : sum)}");
```
```
a.HasValue = False, a ?? -1 = -1, GetValueOrDefault = 0
a.Value: InvalidOperationException
a.HasValue = True, a.Value = 5
5 + null = null
```

- `int?` 赋给 `int` 要显式处理：`int x = a ?? 0;` 或 `a.Value`（为 null 时抛异常）。
- 引用类型后面的 `?`（`string?`）是另一回事：**可空引用类型**，只是给编译器看的标注，见[第 9 章](09-modern-syntax.md)。

### 7.3 `??`、`??=`、`?.` 🟢

| 运算符 | 含义 | 等价写法 |
|---|---|---|
| `a ?? b` | `a` 不是 null 就用 `a`，否则用 `b` | `a != null ? a : b` |
| `a ??= b` | `a` 是 null 时才赋值为 `b` | `if (a == null) a = b;` |
| `a?.B` | `a` 是 null 就返回 null，否则取 `a.B` | `a == null ? null : a.B` |
| `a?[i]` | 同上，用于索引 | |

```csharp
int? x = null;
int y = x ?? 100;                 // 练习 05
Console.WriteLine($"y={y}");

string? name = null;
name ??= "default";
name ??= "second";                // name 已经有值，不会再赋
Console.WriteLine($"name = {name}");

string? s = null;
int? len = s?.Length;             // 注意结果类型变成了 int?
Console.WriteLine($"s?.Length = {(len is null ? "null" : len)}");
Console.WriteLine($"s?.Length ?? 0 = {s?.Length ?? 0}");

int[]? arr = null;
Console.WriteLine($"arr?[0] ?? -1 = {arr?[0] ?? -1}");

Person? p = new Person();         // p.Address 是 null
Console.WriteLine($"p?.Address?.City ?? \"未知\" = {p?.Address?.City ?? "未知"}");
```
```
y=100
name = default
s?.Length = null
s?.Length ?? 0 = 0
arr?[0] ?? -1 = -1
p?.Address?.City ?? "未知" = 未知
```

`?.` 和 `??` 经常连用：`p?.Address?.City ?? "未知"`。Java 里同样的逻辑要写一串 `if (p != null && p.getAddress() != null)`。

调用可能为 null 的委托/事件也用它：`NameChanged?.Invoke(this, EventArgs.Empty);`（[第 6 章](06-delegates-events.md)）。

### 7.4 其他小运算符 ⚪

练习 05 里还有这些，后面章节会细讲：

| 运算符 | 作用 | 详见 |
|---|---|---|
| `obj is string s` | 判断类型并转换赋值 | [第 4 章](04-oop.md)、[第 9 章](09-modern-syntax.md) |
| `obj as string` | 转换失败返回 null，不抛异常 | [第 4 章](04-oop.md) |
| `typeof(int)` | 拿到类型对象，只能用于类型名 | [第 10 章](10-reflection-attributes.md) |
| `sizeof(int)` | 值类型占几个字节 | — |
| `&`、`*` | 指针，要 `unsafe` + `AllowUnsafeBlocks`，业务代码基本不用 | — |

## 8. 枚举 enum 🟢

### 8.1 基本用法

C# 的枚举本质是**有名字的整数**，默认底层类型是 `int`，从 0 开始。这一点和 Java 不同：Java 的 enum 是完整的类，可以有字段和方法；C# 的 enum 不能有方法（要加方法用扩展方法，见[第 3 章](03-classes.md)）。

练习 07：

```csharp
Color c = Color.Red;
Console.WriteLine(c);

Type colorType = typeof(Color);
Console.WriteLine($"BaseType: {colorType.BaseType}");
Console.WriteLine($"GetUnderlyingType(): {Enum.GetUnderlyingType(colorType)}");
Console.WriteLine($"GetName(1): {Enum.GetName((Color)1)}");
Console.WriteLine($"GetNames: {string.Join(", ", Enum.GetNames<Color>())}");
foreach (var value in Enum.GetValues<Color>())
    Console.Write($"{value}={(int)value} ");
Console.WriteLine();

Color red = Enum.Parse<Color>("Red");                      // 失败抛异常
Console.WriteLine($"Parse: {red}");
if (Enum.TryParse<Color>("Purple", out Color colorResult)) // 失败返回 false
    Console.WriteLine($"TryParse: {colorResult}");
else
    Console.WriteLine("TryParse: cannot convert Purple");

Console.WriteLine($"(Color)2: {(Color)2}");

enum Color { Red, Green, Blue }
```
```
Red
BaseType: System.Enum
GetUnderlyingType(): System.Int32
GetName(1): Green
GetNames: Red, Green, Blue
Red=0 Green=1 Blue=2 
Parse: Red
TryParse: cannot convert Purple
(Color)2: Blue
```

| 要做什么 | 写法 |
|---|---|
| 枚举 → 字符串 | `c.ToString()` 或直接插值 `$"{c}"` |
| 枚举 → 数字 | `(int)c` |
| 数字 → 枚举 | `(Color)2` |
| 字符串 → 枚举 | `Enum.Parse<Color>("Red")` / `Enum.TryParse<Color>(s, out var c)` |
| 所有值 / 所有名字 | `Enum.GetValues<Color>()` / `Enum.GetNames<Color>()` |
| 数字是否是定义过的值 | `Enum.IsDefined((Color)99)` |
| 指定底层类型和值 | `enum Status : byte { Inactive = 0, Active = 10 }` |

⚠️ 任何整数都能强转成枚举，**不会报错**：

```csharp
Console.WriteLine((Color)99);
Console.WriteLine(Enum.IsDefined((Color)99));
```
```
99
False
```

从数据库、接口参数拿到的数字转成枚举后，要用 `Enum.IsDefined` 校验一下。

### 8.2 [Flags] 位标志 🟡

一个变量要同时表示多个选项（权限、星期几）时，用 `[Flags]`，每个值是 2 的幂，用 `|` 组合：

```csharp
var p = Permission.Read | Permission.Write;
Console.WriteLine(p);
Console.WriteLine((int)p);
Console.WriteLine($"HasFlag(Write) = {p.HasFlag(Permission.Write)}");
Console.WriteLine($"(p & Write) != 0 : {(p & Permission.Write) != 0}");
p |= Permission.Delete;   // 加权限
Console.WriteLine(p);
p &= ~Permission.Write;   // 去掉权限
Console.WriteLine(p);
Console.WriteLine((Permission)0);

[Flags]
enum Permission
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    All = Read | Write | Delete
}
```
```
Read, Write
3
HasFlag(Write) = True
(p & Write) != 0 : True
All
Read, Delete
None
```

注意第 5 行：`Read | Write | Delete` 正好等于定义好的 `All`，`ToString()` 就直接打印 `All`。

| 操作 | 写法 |
|---|---|
| 组合 | `a \| b` |
| 判断有没有 | `p.HasFlag(X)` 或 `(p & X) != 0` |
| 加上 | `p \|= X` |
| 去掉 | `p &= ~X` |

⚠️ 练习 07 里的 `MultiRole` 把 `Insert` 定义成了 0：

```csharp
for (int j = 0; j < 8; j++)
    Console.WriteLine("{0,3} - {1:G}", j, (MultiRole)j);

[Flags]
enum MultiRole { Insert = 0, Delete = 1, Update = 2, Select = 4 }
```
```
  0 - Insert
  1 - Delete
  2 - Update
  3 - Delete, Update
  4 - Select
  5 - Delete, Select
  6 - Update, Select
  7 - Delete, Update, Select
```

0 表示"一个都没选"，`Insert` 永远组合不进去，`HasFlag(MultiRole.Insert)` 对任何值都是 `True`。**Flags 枚举里 0 只能留给 `None`**，其他值从 1 开始按 1、2、4、8 递增。

## 9. 小结

1. C# 的 `int`、`string` 是 `System.Int32`、`System.String` 的别名；`byte` 是无符号的；没有 `int`/`Integer` 两套类型。
2. 金额一律用 `decimal`（字面量加 `m`），不要用 `double`。
3. 字符串优先用插值 `$"..."`，格式写在 `{值:F2}` 里；循环拼接用 `StringBuilder`。
4. `var` 是编译时推断、强类型；`dynamic` 才是运行时绑定，写错了运行才报错，少用。
5. 用户输入转数字用 `TryParse`；`(int)` 截断、`Convert` 银行家舍入；整数溢出默认静默回绕，关键计算包 `checked`。
6. 编译期常量用 `const`，运行时确定的只读值用 `static readonly`；公开给别的程序集的常量慎用 `const`。
7. `int?` 表示可以为 null 的值类型；`??`、`??=`、`?.` 让判空代码短很多。
8. 枚举就是带名字的整数，任何数字都能强转进去；`[Flags]` 枚举值用 1、2、4、8，0 留给 `None`。

---

[目录](../README.md) · 下一章：[值类型与引用类型 🟢](02-value-and-reference.md)
