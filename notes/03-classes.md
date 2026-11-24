# 第 3 章 类与对象 🟢

> 本章练习：[09-class-members.cs](../basics/09-class-members.cs)、[10-constructor-finalizer.cs](../basics/10-constructor-finalizer.cs)、[11-operator-overloading.cs](../basics/11-operator-overloading.cs)、[15-extension-methods.cs](../basics/15-extension-methods.cs)、[17-properties.cs](../basics/17-properties.cs)、[18-required-init.cs](../basics/18-required-init.cs)

> 从 Java 过来写 C# 的类，你会遇到这些问题：
> 为什么 C# 代码里看不到 `getName()` / `setName()`；`{ get; } = DateTime.Now` 和 `=> DateTime.Now` 看着差不多，结果却不一样；不写访问修饰符默认是什么；构造函数怎么调用另一个构造函数、怎么调父类构造函数；`~ClassName()` 是不是像 C++ 的析构函数；怎么给 `string` 这种不能改的类"加"一个方法；`required`、`init` 是干什么的。
> 本章讲清楚：类里能写哪些成员、属性的各种写法、访问修饰符、构造函数和初始化顺序、终结器和 GC、对象初始化器、运算符重载、扩展方法、`partial` 和 `static class`。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 类的成员一览 🟢

练习 09 把一个类能写的成员都列了出来：

```csharp
class ClassName
{
    public const int ConstName = 123;                // 常量
    private string _fieldName = string.Empty;        // 字段
    private readonly string[] _items = new string[10];

    public ClassName() { }                           // 构造函数
    ~ClassName() { }                                 // 终结器，实际很少用

    public string PropertyName                       // 属性
    {
        get => _fieldName;
        set
        {
            _fieldName = value;
            NameChanged?.Invoke(this, EventArgs.Empty);   // 触发事件
        }
    }

    public string this[int i]                        // 索引器
    {
        get => _items[i];
        set => _items[i] = value;
    }

    public void MethodName(string args) { }          // 方法

    public event EventHandler? NameChanged;          // 事件
}
```

```csharp
var demo = new ClassName();
demo[0] = "zero";
Console.WriteLine($"demo[0] = {demo[0]}");
demo.NameChanged += (sender, e) => Console.WriteLine("事件触发：PropertyName 变了");
demo.PropertyName = "new value";
```
```
demo[0] = zero
事件触发：PropertyName 变了
```

| 成员 | 作用 | Java 对应 |
|---|---|---|
| 字段 | 存数据，一般 `private` | 字段 |
| 常量 `const` | 编译期常量（[第 1 章](01-types-and-syntax.md)） | `static final` |
| **属性** | 对外暴露数据，像字段一样用，背后是 get/set 方法 | `getXxx()` / `setXxx()` |
| 方法 | 行为 | 方法 |
| 构造函数 | 创建对象时初始化 | 构造方法 |
| 终结器 `~X()` | 对象被 GC 回收前调用 | `finalize()`（已废弃） |
| **索引器** `this[...]` | 让对象支持 `obj[i]` | 没有 |
| 事件 `event` | 发布/订阅通知（[第 6 章](06-delegates-events.md)） | 没有，要自己写监听器列表 |
| 运算符 `operator` | 让对象支持 `+`、`==` 等 | 没有 |

命名习惯：私有字段 `_camelCase`，其他（属性、方法、类、常量）都用 `PascalCase`。Java 的方法名是小驼峰，C# 是**大驼峰**：`GetAge()`、`ToString()`。

## 2. 属性 🟢

### 2.1 属性代替 Get/Set 方法

练习 17，Java 风格和 C# 风格对比：

```csharp
class Person
{
    private string name = string.Empty;

    // Java 风格的读写方法
    public string GetName() => name;
    public void SetName(string value) => name = value;

    // C# 属性：编译后其实就是 get_Name() 和 set_Name(value) 两个方法
    public string Name
    {
        get { return name; }
        set { name = value; }      // value 是赋过来的值
    }
}

var p1 = new Person();
p1.SetName("Owen");
Console.WriteLine(p1.GetName());
p1.Name = "Owen";                  // 用起来像字段
Console.WriteLine(p1.Name);
```
```
Owen
Owen
```

属性用起来像字段，但本质是方法，所以可以在 `set` 里做校验、触发事件，以后加逻辑也不用改调用方。**C# 里对外公开的数据一律用属性，不要用 public 字段。**

### 2.2 自动属性

没有额外逻辑时，不用自己写字段，编译器会生成一个隐藏字段（练习 09）：

```csharp
public int Age { get; set; }
```

各种写法速查：

| 写法 | 含义 |
|---|---|
| `public int Age { get; set; }` | 可读可写 |
| `public int Age { get; set; } = 18;` | 带初始值 |
| `public int Age { get; private set; }` | 外部只读，类内部可以改 |
| `public int Age { get; }` | 只读，只能在构造函数或声明处赋值 |
| `public int Age { get; init; }` | 只能在创建对象时赋值（见 2.4） |
| `public required int Age { get; init; }` | 创建对象时**必须**赋值（见 2.4） |
| `public string Name => $"{First} {Last}";` | 计算属性，没有字段，每次读取都算一次 |

### 2.3 `{ get; } =` 和 `=>` 的区别 ⚠️

两种写法长得很像，含义完全不同。练习 17：

```csharp
class LoginInfo
{
    public DateTime Birthday { get; } = DateTime.Now;   // 创建对象时算一次，存进隐藏字段
    public DateTime LoginTime => DateTime.Now;          // 没有字段，每次读取都执行一次
}

var login = new LoginInfo();
Console.WriteLine($"Birthday={login.Birthday:HH:mm:ss.fff}, LoginTime={login.LoginTime:HH:mm:ss.fff}");
Thread.Sleep(1000);
Console.WriteLine($"Birthday={login.Birthday:HH:mm:ss.fff}, LoginTime={login.LoginTime:HH:mm:ss.fff}");
```
```
Birthday=17:30:18.251, LoginTime=17:30:18.307
Birthday=17:30:18.251, LoginTime=17:30:19.313
```

| | `{ get; } = 表达式` | `=> 表达式` |
|---|---|---|
| 有没有字段 | 有（编译器生成） | 没有 |
| 表达式什么时候执行 | 创建对象时，**一次** | **每次**读取属性时 |
| 适合 | 固定的初始值 | 由其他字段算出来的值 |

⚠️ 典型 bug：`public List<string> Items => new();` 每次访问都返回一个新的空列表，`obj.Items.Add("x")` 加进去就丢了。应该写成 `{ get; } = new();`。

计算属性的完整写法和简写（练习 17）：

```csharp
class FullNamePerson
{
    private readonly string firstName;
    private readonly string lastName;

    public FullNamePerson(string firstName, string lastName)
    {
        this.firstName = firstName;
        this.lastName = lastName;
    }

    public string FullName                         // 完整写法
    {
        get { return $"Full Name: {lastName} {firstName}"; }
    }

    public string Initials => $"{firstName[0]}.{lastName[0]}.";   // 简写
}

var p2 = new FullNamePerson("Owen", "Yin");
Console.WriteLine(p2.FullName);
Console.WriteLine(p2.Initials);
// p2.FullName = "Zuowen Yin";   // ❌ 只有 get，不能赋值
```
```
Full Name: Yin Owen
O.Y.
```

### 2.4 private set、init、required 🟢

练习 18：

```csharp
class Person
{
    public DateTime Birthday { get; private set; }
    public required int Age { get; init; }

    public Person() { }

    [SetsRequiredMembers]
    public Person(DateTime birthday)
    {
        Birthday = birthday;
        Age = DateTime.Today.Year - birthday.Year;
    }

    [SetsRequiredMembers]
    public Person(int age) { Age = age; }

    public void SetBirthday(DateTime birthday) { Birthday = birthday; }
}
```

```csharp
var p1 = new Person { Age = 20 };
// var p0 = new Person();              // ❌ CS9035：必须给 required 的 Age 赋值
Console.WriteLine($"p1: Age={p1.Age}, Birthday={p1.Birthday:yyyy-MM-dd}");

// p1.Age = 30;                        // ❌ CS8852：init 属性创建后不能改
// p1.Birthday = DateTime.Today;       // ❌ 外部不能赋值
p1.SetBirthday(DateTime.Parse("2006-01-01"));
Console.WriteLine($"p1: Birthday={p1.Birthday:yyyy-MM-dd}");

var p2 = new Person(DateTime.Parse("1988-12-28"));   // 构造函数已承诺给 Age 赋值
Console.WriteLine($"p2: Age={p2.Age}, Birthday={p2.Birthday:yyyy-MM-dd}");
var p3 = new Person(18);
Console.WriteLine($"p3: Age={p3.Age}");
```
```
p1: Age=20, Birthday=0001-01-01
p1: Birthday=2006-01-01
p2: Age=38, Birthday=1988-12-28
p3: Age=18
```

| 写法 | 谁能赋值 | 什么时候能赋值 |
|---|---|---|
| `{ get; set; }` | 任何人 | 任何时候 |
| `{ get; private set; }` | 只有类自己 | 任何时候 |
| `{ get; }` | 只有类自己 | 只在构造函数里 |
| `{ get; init; }` | 任何人 | 只在构造函数和**对象初始化器** `{ }` 里 |
| `required ... { get; init; }` | 任何人 | 同上，而且**必须**赋值，否则编译错误 |

- `init` 让你能用对象初始化器 `new Person { Age = 20 }` 创建**不可变对象**，不用写一个参数很多的构造函数。
- `required` 解决"忘了给必填属性赋值"的问题，编译器帮你检查。
- `[SetsRequiredMembers]`（在 `System.Diagnostics.CodeAnalysis` 里）标在构造函数上，告诉编译器"这个构造函数会给所有 required 成员赋值"。⚠️ 编译器**不验证**这个承诺，构造函数里漏了也不会报错。
- `DateTime` 没赋值时是默认值 `0001-01-01`，不是 null。

## 3. 访问修饰符 🟢

### 3.1 对照表

| C# | 谁能访问 | Java 对应 |
|---|---|---|
| `public` | 所有人 | `public` |
| `private` | 只有本类 | `private` |
| `protected` | 本类 + 子类（**同一程序集的非子类不行**） | `protected`（Java 里同包也能访问） |
| `internal` | 同一个**程序集**（项目 / dll）里 | 包级私有（不写修饰符） |
| `protected internal` | 同一程序集 **或** 子类 | 最接近 Java 的 `protected` |
| `private protected` | 同一程序集里的子类 | 没有 |

程序集：一个 `.csproj` 编译出来的一个 `.dll` 或 `.exe`。`internal` 的范围是整个项目，不是某个命名空间。

### 3.2 默认值 ⚠️

| 位置 | 不写修饰符时默认 |
|---|---|
| 类的成员（字段、方法、属性……） | **`private`** |
| 顶层的类、struct、接口、enum | **`internal`** |
| 接口的成员 | `public` |

和 Java 不同：Java 不写是"包级私有"，C# 成员不写是 `private`。

```csharp
var d = new Derived();
d.Test();
Console.WriteLine(typeof(Default).IsPublic ? "Default 是 public" : "Default 是 internal（顶层类默认）");
var f = typeof(Default).GetField("x", BindingFlags.NonPublic | BindingFlags.Instance)!;
Console.WriteLine($"x 字段：IsPrivate={f.IsPrivate}");

class Base
{
    private int a = 1;
    protected int b = 2;
    internal int c = 3;
    protected internal int d = 4;
    private protected int e = 5;
    public int f = 6;
}
class Derived : Base
{
    public void Test()
    {
        // Console.WriteLine(a);   // ❌ private
        Console.WriteLine($"b={b}, c={c}, d={d}, e={e}, f={f}");
    }
}
class Default
{
    int x = 0;   // 没写修饰符
    public int X => x;
}
```
```
b=2, c=3, d=4, e=5, f=6
Default 是 internal（顶层类默认）
x 字段：IsPrivate=True
```

（子类和父类在同一个程序集里，所以 `c`、`d`、`e` 都能访问。）

怎么选：**能 `private` 就 `private`**；需要给项目里其他类用就 `internal`；只有真正对外的 API 才 `public`。

## 4. 构造函数 🟢

### 4.1 this 和 base

练习 10：

```csharp
class Person
{
    public static int Count;
    public string Name;
    private int age;

    static Person()                   // 静态构造函数
    {
        Count = 0;
        Console.WriteLine("Person 静态构造函数执行");
    }

    public Person(string name, int age)
    {
        Name = name;                  // 大小写不同，不需要 this
        this.age = age;               // 同名，用 this 区分字段和参数
        Count++;
    }

    public void Show() => Console.WriteLine($"{GetType().Name}: {Name}, {age} 岁, 已创建 {Count} 个");
}

class Male : Person
{
    // 父类没有无参构造函数，子类必须用 : base(...) 把参数传过去（相当于 Java 的 super）
    public Male(string name, int age) : base(name, age)
    {
        Console.WriteLine("Male 构造函数执行");
    }
}

var person = new Person("Owen", 36);
person.Show();
var male = new Male("Tom", 20);
male.Show();
```
```
Person 静态构造函数执行
Person: Owen, 36 岁, 已创建 1 个
Male 构造函数执行
Male: Tom, 20 岁, 已创建 2 个
```

| | Java | C# |
|---|---|---|
| 调用本类另一个构造函数 | 方法体第一行 `this(...)` | 签名后面 `: this(...)` |
| 调用父类构造函数 | 方法体第一行 `super(...)` | 签名后面 `: base(...)` |
| 调用父类方法 | `super.foo()` | `base.Foo()` |

父类没有无参构造函数、子类又没写 `: base(...)` 时，编译错误 CS7036。

### 4.2 构造函数链和初始化顺序 🟡

用 `: this(...)` 把多个构造函数串起来，公共逻辑只写一处：

```csharp
class Account
{
    public string Owner { get; }
    public decimal Balance { get; private set; }
    static Account() => Console.WriteLine("Account 静态构造函数");

    public Account(string owner) : this(owner, 0m)    // 先执行另一个构造函数
    {
        Console.WriteLine("Account(owner) 执行");
    }

    public Account(string owner, decimal balance)
    {
        Console.WriteLine("Account(owner, balance) 执行");
        Owner = owner;
        Balance = balance;
    }
    public override string ToString() => $"{GetType().Name}: {Owner} {Balance}";
}

class Savings : Account
{
    private readonly decimal _rate = Log("Savings 字段初始化");
    static Savings() => Console.WriteLine("Savings 静态构造函数");
    public Savings(string owner, decimal balance) : base(owner, balance)
    {
        Console.WriteLine("Savings 构造函数体");
    }
    static decimal Log(string msg) { Console.WriteLine(msg); return 0.01m; }
}

var a = new Account("Owen");
Console.WriteLine(a);
var b = new Account("Tom", 100m);
Console.WriteLine(b);
Console.WriteLine("----");
var s = new Savings("Amy", 50m);
Console.WriteLine(s);
```
```
Account 静态构造函数
Account(owner, balance) 执行
Account(owner) 执行
Account: Owen 0
Account(owner, balance) 执行
Account: Tom 100
----
Savings 静态构造函数
Savings 字段初始化
Account(owner, balance) 执行
Savings 构造函数体
Savings: Amy 50
```

从输出能看出 `new Savings(...)` 的顺序：

1. 子类静态构造函数（第一次用到这个类时，只执行一次）
2. **子类的字段初始化器**
3. 父类构造函数
4. 子类构造函数体

⚠️ 第 2、3 步和 Java 相反：Java 是先 `super()`，再初始化子类字段；C# 是**先初始化子类字段，再调父类构造函数**。所以字段初始化器里不能用 `this`（编译器也不允许）。

### 4.3 静态构造函数

- 写法：`static 类名() { }`，没有访问修饰符、没有参数。
- 第一次创建实例或访问静态成员之前，由运行时**自动调用一次**，不能手动调用。
- 用来初始化 `static readonly` 字段（[第 1 章](01-types-and-syntax.md) 的 `MyMath.PI2`）。
- 相当于 Java 的 `static { }` 静态代码块。
- ⚠️ 静态构造函数里抛异常，这个类在整个程序运行期间都不能用了（之后每次访问都抛 `TypeInitializationException`）。里面别放可能失败的逻辑。

### 4.4 字段的默认值

没有赋值的字段会被自动初始化成默认值（局部变量不会，局部变量不赋值就用是编译错误）：

```csharp
class Empty
{
    private int _n;
    private string? _s;
    private bool _b;
    private decimal _m;
    public void Show() => Console.WriteLine($"字段默认值: int={_n}, string={(_s is null ? "null" : _s)}, bool={_b}, decimal={_m}");
}
```
```
字段默认值: int=0, string=null, bool=False, decimal=0
```

## 5. 终结器与 GC 🟡

### 5.1 终结器不是析构函数

`~Movie()` 叫**终结器**（finalizer，老资料叫"析构函数"），由 GC 在回收对象前调用。和 C++ 的析构函数不同，**什么时候调用完全不确定**。练习 10：

```csharp
class Movie
{
    public Movie(DateTime dateTime) { DateTime = dateTime; }
    ~Movie()
    {
        Console.WriteLine($"影片撤档时间 {DateTime.Now:HH:mm:ss}");
    }
    public DateTime DateTime { get; }
}

PlayMovie();                    // 方法结束后，里面的 Movie 对象就没人引用了
GC.Collect();                   // 手动触发 GC
GC.WaitForPendingFinalizers();  // 等终结器线程跑完

static void PlayMovie()
{
    var temp = new Movie(DateTime.Now);
    Console.WriteLine($"临时影片上映时间 {temp.DateTime:HH:mm:ss}");
}
```
```
临时影片上映时间 17:30:16
影片撤档时间 17:30:16
```

如果不手动 GC 会怎样？

```csharp
Create();
Console.WriteLine("Create 结束，没有调 GC");
GC.Collect();
GC.WaitForPendingFinalizers();
Console.WriteLine("GC 之后");
Create();
Console.WriteLine("程序结束（第二个对象不手动 GC）");

static void Create() { var r = new Res(); }

class Res
{
    private static int _n;
    private readonly int _id = ++_n;
    ~Res() => Console.WriteLine($"Res#{_id} 终结器执行");
}
```
```
Create 结束，没有调 GC
Res#1 终结器执行
GC 之后
程序结束（第二个对象不手动 GC）
```

`Res#2` 的终结器**根本没有执行**：.NET Core 起，程序退出时不会再跑终结器。

### 5.2 为什么不要依赖终结器 ⚠️

| 问题 | 说明 |
|---|---|
| 时间不确定 | 什么时候 GC 由运行时决定，可能很久以后，也可能直到程序退出都不跑 |
| 不保证执行 | 程序退出时不执行（上面实测） |
| 在另一个线程上执行 | 终结器线程，里面抛异常会让整个进程崩溃 |
| 拖慢 GC | 有终结器的对象至少要两轮 GC 才能真正回收 |

所以：

- 普通对象**不需要**写终结器，内存交给 GC 就行。
- 文件、数据库连接、网络连接这些要**及时释放**的资源，实现 `IDisposable`，用 `using` 释放（[第 8 章](08-exceptions-resources.md)）。相当于 Java 的 `AutoCloseable` + try-with-resources。
- 业务代码里也不要调 `GC.Collect()`，上面只是为了演示。

练习 10 的注释里还有一个细节：Debug 模式下，局部变量会一直存活到所在方法结束。所以要把 `new Movie` 放进单独的方法 `PlayMovie()`，方法返回后对象才能被回收。

## 6. 对象初始化器 🟢

先调构造函数，再在 `{ }` 里给属性赋值。练习 09 和 11：

```csharp
var p2 = new Person("Owen") { Age = 38 };         // 构造函数 + 对象初始化器
Movie movie1 = new Movie("Owen");                 // 构造函数传参
Movie movie2 = new Movie { Film = "Yin" };        // 无参构造 + 初始化器
Movie movie0 = new() { Film = "Tom" };            // 左边写了类型，右边可以只写 new()
```

```
var p2 = new Person { Age = 38 };
等价于
var p2 = new Person();
p2.Age = 38;
```

还可以嵌套，以及给集合属性 `Add`：

```csharp
var team = new Team
{
    Name = "Dev",
    Leader = { Name = "Owen" },        // 没有 new：给已有的 Leader 对象的属性赋值
    Members = { "Tom", "Amy" }         // 没有 new：调用 Members.Add(...)
};
Console.WriteLine($"{team.Name}, leader={team.Leader.Name}, members={string.Join("/", team.Members)}");

class Leader { public string Name { get; set; } = ""; }
class Team
{
    public string Name { get; set; } = "";
    public Leader Leader { get; } = new();
    public List<string> Members { get; } = new();
}
```
```
Dev, leader=Owen, members=Tom/Amy
```

Java 里要么写一堆 setter 调用，要么写 Builder。C# 用对象初始化器就够了，再配合 `init` / `required` 还能保证不可变和必填。

## 7. 索引器 🟡

索引器让对象可以像数组、字典一样用 `obj[...]` 访问，参数可以是任意类型，也可以重载：

```csharp
class Week
{
    private readonly string[] _days = ["Sun", "Mon", "Tue"];
    public int this[string name] => Array.IndexOf(_days, name);   // 按名字查下标
    public string this[int i] => _days[i];                        // 按下标查名字
}

var w = new Week();
Console.WriteLine(w["Mon"]);
Console.WriteLine(w[1]);
```
```
1
Mon
```

`List<T>` 的 `list[0]`、`Dictionary` 的 `dict["key"]` 都是索引器。Java 里只能写 `list.get(0)`、`map.get("key")`。

## 8. 运算符重载 🟡

### 8.1 写法

练习 11：

```csharp
class Movie
{
    public Movie() { }
    public Movie(string film) { Film = film; }
    public string Film { get; set; } = string.Empty;

    // 必须是 public static，参数里至少有一个是当前类型
    public static Movie operator +(Movie left, Movie right)
        => new Movie { Film = left.Film + ", " + right.Film };

    // == 和 != 必须成对重载，同时建议重写 Equals 和 GetHashCode
    public static bool operator ==(Movie? left, Movie? right) => left?.Film == right?.Film;
    public static bool operator !=(Movie? left, Movie? right) => !(left == right);
    public override bool Equals(object? obj) => obj is Movie m && m.Film == Film;
    public override int GetHashCode() => Film.GetHashCode();
}

Movie movie3 = movie1 + movie2;
Console.WriteLine(movie3.Film);
Console.WriteLine((movie3 + movie0).Film);
Console.WriteLine(movie1 == new Movie("Owen"));   // 重载了 ==，按片名比较
Console.WriteLine(movie1 == movie2);
```
```
Owen, Yin
Owen, Yin, Tom
True
False
```

规则：

- 只写 `==` 不写 `!=`，编译错误 CS0216。`<` 和 `>`、`<=` 和 `>=` 也要成对。
- 重载了 `==`，就要同时重写 `Equals` 和 `GetHashCode`，否则放进 `Dictionary`、`HashSet` 时行为不一致（编译器会给警告）。
- `+=` 不能单独重载，重载了 `+` 就自动有 `+=`。

### 8.2 适合的场景

更适合用在表示"值"的类型上，比如向量、金额、坐标：

```csharp
readonly struct Vec(int x, int y)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public static Vec operator *(Vec a, int k) => new(a.X * k, a.Y * k);
    public static bool operator ==(Vec a, Vec b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vec a, Vec b) => !(a == b);
    public override bool Equals(object? o) => o is Vec v && v == this;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";
}

var v = new Vec(1, 2) * 3;
Console.WriteLine(v);
Console.WriteLine(v == new Vec(3, 6));
```
```
(3, 6)
True
```

（`struct Vec(int x, int y)` 是 C# 12 的**主构造函数**，参数直接写在类型名后面。）

还可以定义类型转换运算符：

```csharp
readonly struct Celsius(double value)
{
    public double Value { get; } = value;
    public static implicit operator Celsius(double v) => new(v);      // 隐式：double → Celsius
    public static explicit operator double(Celsius c) => c.Value;     // 显式：Celsius → double
}

Celsius c = 36.5;
double d = (double)c;
Console.WriteLine($"{c.Value} {d}");
```
```
36.5 36.5
```

⚠️ 别滥用。`movie1 + movie2` 是什么意思，读代码的人得去翻定义才知道，普通业务类用一个有名字的方法（`Merge`）更清楚。只想要"按值比较"的类，用 `record` 更省事，编译器自动生成 `==`、`Equals`、`GetHashCode`（[第 9 章](09-modern-syntax.md)）。

## 9. 扩展方法 🟢

### 9.1 写法

给已有类型"加"方法，不用改原来的类，也不用继承。练习 15：

```csharp
// 必须定义在 非嵌套、非泛型 的 static class 里
public static class PersonExtensions
{
    // 第一个参数前加 this：表示这是 Person 的扩展方法
    public static int GetAge(this Person person)
    {
        var today = DateTime.Today;
        int age = today.Year - person.Birthday.Year;
        if (today < person.Birthday.AddYears(age)) age--;   // 今年生日还没到
        return age;
    }
}

public static class StringExtensions
{
    public static bool IsEmpty(this string s) => string.IsNullOrEmpty(s);
}

// sealed：不能被继承，但仍然可以通过扩展方法添加功能
public sealed class Person
{
    public DateTime Birthday { get; set; }
}
```

```csharp
Person person = new Person { Birthday = DateTime.Parse("1988-12-28") };
Console.WriteLine($"年龄：{person.GetAge()}");                    // 看起来像 Person 自己的方法
Console.WriteLine($"年龄：{PersonExtensions.GetAge(person)}");     // 编译器实际调用的是这个
Console.WriteLine($"\"\".IsEmpty() = {"".IsEmpty()}");
Console.WriteLine($"\"hi\".IsEmpty() = {"hi".IsEmpty()}");

int[] nums = [1, 2, 3];
Console.WriteLine(string.Join(", ", nums.Where(x => x > 1)));     // LINQ 也是扩展方法
```
```
年龄：37
年龄：37
"".IsEmpty() = True
"hi".IsEmpty() = False
2, 3
```

（年龄是按运行当天算的。）

### 9.2 要点

- 扩展方法只是**语法糖**：`person.GetAge()` 编译后就是 `PersonExtensions.GetAge(person)`，相当于 Java 里的工具类静态方法 `StringUtils.isEmpty(s)`，只是调用时写法更顺。
- 它访问不到类的 `private` 成员，也不能真的改变类。
- 定义在嵌套类里，编译错误 CS1109。
- 要能调用，扩展类所在的命名空间必须被 `using` 进来。
- LINQ 的 `Where`、`Select`、`OrderBy` 全是 `IEnumerable<T>` 的扩展方法（[第 7 章](07-collections-linq.md)）。
- 给 enum 加"方法"也靠它（C# 的 enum 不能写方法）：

```csharp
static class EnumExt
{
    public static string ToChinese(this Color c) => c switch { Color.Red => "红", _ => "其他" };
}
Console.WriteLine(Color.Red.ToChinese());
```
```
红
```

## 10. partial 与 static class 🟡

### 10.1 partial：一个类分几处写

```csharp
public partial class Order
{
    public int Id { get; init; }
    public string Customer { get; init; } = "";
}

public partial class Order        // 同一个类的另一部分（实际一般在另一个文件里）
{
    public string Describe() => $"Order #{Id} by {Customer}";
}

var o = new Order { Id = 1, Customer = "Owen" };
Console.WriteLine(o.Describe());
```
```
Order #1 by Owen
```

编译时所有 `partial` 部分合并成一个类。主要用途：**代码生成**。工具生成的代码（WinForms 设计器、EF Core、源生成器）放在一个文件，自己写的放在另一个文件，重新生成时不会覆盖你的代码。自己手写的类一般不需要拆。

### 10.2 static class：工具类

```csharp
public static class MathHelper
{
    public static int Square(int x) => x * x;
}

Console.WriteLine(MathHelper.Square(5));
// var h = new MathHelper();   // ❌ CS0712：不能创建静态类的实例
```
```
25
```

- 只能包含静态成员，不能 `new`，不能被继承。
- 相当于 Java 里"`final` 类 + `private` 构造函数 + 全是 `static` 方法"的工具类写法，C# 一个关键字搞定。
- 扩展方法必须放在 static class 里。
- 系统里的 `Math`、`Console`、`File` 都是 static class。

## 11. 小结

1. C# 用**属性**代替 Java 的 getter/setter；公开数据一律用属性，不用 public 字段；没有额外逻辑就用自动属性 `{ get; set; }`。
2. `{ get; } = 值` 只在创建时算一次，`=> 表达式` 每次读取都重新算；返回集合的属性别写成 `=> new()`。
3. 不可变对象用 `{ get; init; }` + 对象初始化器；必填属性加 `required`，编译器帮你检查。
4. 成员默认 `private`，顶层类默认 `internal`；`internal` 是"同一个项目"，C# 的 `protected` 不包括同包访问。
5. 构造函数链用 `: this(...)`，调父类用 `: base(...)`；子类字段初始化器**先于**父类构造函数执行，和 Java 相反。
6. 终结器执行时间不确定、程序退出时也不执行，不要依赖它；释放资源用 `IDisposable` + `using`。
7. 运算符重载要成对、要同时重写 `Equals`/`GetHashCode`，只用在"值"类型上。
8. 扩展方法是静态方法的语法糖，用来给不能改的类型（`string`、enum、第三方类）加方法；工具类用 `static class`。

---

上一章：[值类型与引用类型 🟢](02-value-and-reference.md) · [目录](../README.md) · 下一章：[继承、接口与多态 🟢](04-oop.md)
