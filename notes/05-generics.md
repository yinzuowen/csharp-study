# 第 5 章 泛型 🟢

> 本章练习：[25-generics.cs](../basics/25-generics.cs)、[26-variance.cs](../basics/26-variance.cs)

> 泛型在 C# 里比在 Java 里用得更多，因为它**不擦除**，能做的事情多得多。写代码时你会遇到这些问题：
> 写了 `new T()` 编译报 `CS0304`；泛型方法里想 `return null` 报 `CS0403`；约束写成 `where T : new(), class` 报一串 `CS0401`、`CS0449`；`List<string>` 不能赋给 `List<object>`，可 `IEnumerable<string>` 就可以；看到 `IEnumerable<out T>`、`Action<in T>` 里的 `out`/`in` 不知道是什么意思。
> 本章讲清楚：泛型类/方法/接口怎么写、C# 泛型和 Java 擦除式泛型的区别、约束怎么写、`default(T)`、泛型接口的实现方式，以及协变和逆变。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 泛型类、泛型方法、泛型接口、泛型委托

### 1.1 泛型类型

类、结构体、接口、委托都可以带类型参数，语法和 Java 基本一样：

```csharp
var point = new Point<int>(100, 200);
Console.WriteLine(point);
var pointD = new Point<double>(1.5, 2.5);
Console.WriteLine(pointD);

struct Point<T>                        // 泛型结构体（Java 没有 struct）
{
    public T X;
    public T Y;
    public Point(T x, T y) { X = x; Y = y; }
    public override string ToString() => $"({X}, {Y})";
}
```
```
(100, 200)
(1.5, 2.5)
```

### 1.2 泛型方法

类型参数写在**方法名后面**（Java 写在返回值前面）。调用时一般不用写 `<T>`，编译器根据参数推断：

```csharp
int a = 1, b = 2;
Swap(ref a, ref b);              // 等价于 Swap<int>(ref a, ref b)
Console.WriteLine($"a={a}, b={b}");

string x = "x", y = "y";
Swap(ref x, ref y);
Console.WriteLine($"x={x}, y={y}");

static void Swap<T>(ref T x, ref T y)     // Java：static <T> void swap(T x, T y)
{
    (x, y) = (y, x);                       // 元组交换
}
```
```
a=2, b=1
x=y, y=x
```

| | Java | C# |
|---|---|---|
| 泛型方法声明 | `static <T> void swap(...)` | `static void Swap<T>(...)` |
| 显式指定类型参数 | `Util.<Integer>swap(...)` | `Swap<int>(...)` |
| 基本类型作类型参数 | ❌ 只能 `List<Integer>` | ✅ `List<int>` |

### 1.3 泛型委托

```csharp
DeleteHandler<UserEntity> handler1 = OnDelete;            // 方法组
DeleteHandler<UserEntity> handler2 = entity =>            // lambda
{
    Console.WriteLine($"lambda 删除 {entity.Id}");
    return true;
};
handler1(new UserEntity { Id = 1 });
handler2(new UserEntity { Id = 2 });

delegate bool DeleteHandler<T>(T entity);
```
```
删除 1
lambda 删除 2
```

实际项目里基本不用自己定义泛型委托，内置的 `Func<T, bool>`、`Predicate<T>` 签名完全一样，见[第 6 章](06-delegates-events.md)。

### 1.4 泛型接口与 `Service<T> : IService<T>` 🟢

业务代码里最常见的就是"泛型接口 + 泛型实现类"，比如通用仓储/服务：

```csharp
interface IEntity { int Id { get; } }
record User(int Id, string Name) : IEntity;
record Order(int Id, decimal Amount) : IEntity;

interface IService<T> where T : class, IEntity
{
    void Add(T item);
    T? GetById(int id);
    IEnumerable<T> GetAll();
}

// 写法 1：泛型类实现泛型接口，T 原样传过去
class Service<T> : IService<T> where T : class, IEntity
{
    protected readonly List<T> Items = [];
    public void Add(T item) => Items.Add(item);
    public T? GetById(int id) => Items.FirstOrDefault(x => x.Id == id);
    public IEnumerable<T> GetAll() => Items;
}

// 写法 2：普通类继承/实现时直接指定具体类型
class OrderService : Service<Order> { }
```

```csharp
IService<User> userService = new Service<User>();
userService.Add(new User(1, "Owen"));
userService.Add(new User(2, "Tom"));
Console.WriteLine($"Count = {userService.GetAll().Count()}");
Console.WriteLine($"GetById(2) = {userService.GetById(2)}");
Console.WriteLine($"GetById(9) = {userService.GetById(9)?.ToString() ?? "null"}");

IService<Order> orderService = new OrderService();
orderService.Add(new Order(100, 9.9m));
Console.WriteLine($"Order: {orderService.GetById(100)}");
```
```
Count = 2
GetById(2) = User { Id = 2, Name = Tom }
GetById(9) = null
Order: Order { Id = 100, Amount = 9.9 }
```

⚠️ **约束不会继承**。接口上有 `where T : class`，实现类不重写一遍就报错：

```csharp
class Service<T> : IService<T> { }            // 忘了写 where T : class
interface IService<T> where T : class { }
```
```
error CS0452: The type 'T' must be a reference type in order to use it as parameter 'T' in the generic type or method 'IService<T>'
```

> 依赖注入里注册这种泛型服务，可以一次注册"开放泛型"：`services.AddScoped(typeof(IService<>), typeof(Service<>));`，之后要 `IService<User>`、`IService<Order>` 都能自动解析。

## 2. C# 泛型不擦除：和 Java 最大的区别 🟢

Java 的泛型是**类型擦除**：编译后 `List<String>` 和 `List<Integer>` 都变成 `List`，运行时不知道 `T` 是什么。C# 的泛型是**运行时真实存在的**（reified）：`List<int>` 和 `List<string>` 是两个不同的类型。

### 2.1 运行时能拿到 T

```csharp
Console.WriteLine(TypeName<int>());
Console.WriteLine(TypeName<string>());
Console.WriteLine(TypeName<List<int>>());

var ints = new List<int>();
var strs = new List<string>();
Console.WriteLine($"List<int>    的类型：{ints.GetType()}");
Console.WriteLine($"List<string> 的类型：{strs.GetType()}");
Console.WriteLine($"两者是同一个类型吗：{ints.GetType() == strs.GetType()}");

object o = ints;
Console.WriteLine($"o is List<int>：{o is List<int>}");
Console.WriteLine($"o is List<string>：{o is List<string>}");

var arr = CreateArray<string>(3);
Console.WriteLine($"new T[3]：{arr.GetType()}，长度 {arr.Length}");

Counter<int>.Count++;
Counter<int>.Count++;
Counter<string>.Count++;
Console.WriteLine($"Counter<int>.Count={Counter<int>.Count}, Counter<string>.Count={Counter<string>.Count}");

static string TypeName<T>() => $"T = {typeof(T).Name}，是值类型：{typeof(T).IsValueType}";
static T[] CreateArray<T>(int n) => new T[n];
class Counter<T> { public static int Count; }
```
```
T = Int32，是值类型：True
T = String，是值类型：False
T = List`1，是值类型：False
List<int>    的类型：System.Collections.Generic.List`1[System.Int32]
List<string> 的类型：System.Collections.Generic.List`1[System.String]
两者是同一个类型吗：False
o is List<int>：True
o is List<string>：False
new T[3]：System.String[]，长度 3
Counter<int>.Count=2, Counter<string>.Count=1
```

`` List`1 `` 里的 `` `1 `` 表示"有 1 个类型参数"，是 .NET 内部的类型名写法。

| 能不能做 | Java（擦除） | C#（不擦除） |
|---|---|---|
| `typeof(T)` / `T.class` | ❌ 拿不到，要额外传 `Class<T>` | ✅ `typeof(T)` |
| `obj is List<String>` | ❌ 编译不过 | ✅ 能准确判断 |
| `new T[n]` | ❌ | ✅ |
| `new T()` | ❌ 只能反射 | ✅ 加 `new()` 约束即可 |
| 基本类型作类型参数 | ❌ 只能用包装类 | ✅ `List<int>` |
| 泛型类的静态字段 | 所有 `T` 共用一份 | **每个 `T` 各一份** |
| 重载 `f(List<String>)` 和 `f(List<Integer>)` | ❌ 擦除后签名冲突 | ✅ |

⚠️ 最后一个坑：`Counter<int>.Count` 和 `Counter<string>.Count` 是两个不同的静态字段。想让所有 `T` 共享，要把静态字段放到非泛型的类里。

### 2.2 List<int> 没有装箱

Java 的 `List<Integer>` 里每个元素都是一个 `Integer` 对象。C# 的 `List<int>` 底层就是 `int[]`，元素直接存值，**没有装箱**。和非泛型的老集合 `ArrayList`（元素类型是 `object`，存 `int` 要装箱）对比：

```csharp
const int N = 10_000_000;

var list = new List<int>(N);
for (int i = 0; i < N; i++) list.Add(i);
long sum = 0;
foreach (var x in list) sum += x;

var al = new System.Collections.ArrayList(N);
for (int i = 0; i < N; i++) al.Add(i);        // 装箱：每个 int 变成堆上的对象
long sum2 = 0;
foreach (object x in al) sum2 += (int)x;      // 拆箱
```

用 `Stopwatch` 计时、`GC.GetAllocatedBytesForCurrentThread()` 统计分配（先预热一轮）：

```
List<int> :    69 ms, 分配   38 MB
ArrayList :   678 ms, 分配  305 MB
```

- `List<int>`：只有一个 1000 万长度的 `int[]`，4 字节 × 1000 万 ≈ 38 MB。
- `ArrayList`：一个 `object[]`（8 字节引用 × 1000 万）加 1000 万个装箱对象，分配多了约 8 倍，速度慢一个数量级。
- 耗时每次运行会有差别，量级关系是稳定的。

> 装箱/拆箱的原理见[第 2 章](02-value-and-reference.md)。结论：**新代码不要用 `ArrayList`、`Hashtable`**，用 `List<T>`、`Dictionary<TKey, TValue>`。

## 3. 类型参数约束 where 🟢

### 3.1 为什么需要约束

不加约束时，编译器只知道 `T` 是"某个类型"，只能调用 `object` 上的方法：

```csharp
class Repo<T>  { public T Create() => new T(); }
class Repo2<T> { public int GetId(T x) => x.Id; }
class Repo3<T> { public T? Find() => null; }
```
```
error CS0304: Cannot create an instance of the variable type 'T' because it does not have the new() constraint
error CS1061: 'T' does not contain a definition for 'Id' and no accessible extension method 'Id' accepting a first argument of type 'T' could be found
error CS0403: Cannot convert null to type parameter 'T' because it could be a non-nullable value type. Consider using 'default(T)' instead.
```

加上约束就都可以了（练习里的 `Repo<T>`）：

```csharp
class Repo<T> where T : class, IEntity, new()
{
    private readonly List<T> _items = [];

    public T Create(int id)
    {
        var item = new T();     // 靠 new() 约束
        item.Id = id;           // 靠 IEntity 约束
        _items.Add(item);
        return item;
    }

    public T? Find(int id) => _items.FirstOrDefault(x => x.Id == id);   // 找不到返回 null，靠 class 约束
}
```
```csharp
var repo = new Repo<UserEntity>();
repo.Create(10);
repo.Create(20);
Console.WriteLine($"Find(20): {repo.Find(20)?.Id}");
Console.WriteLine($"Find(99): {repo.Find(99)?.Id ?? -1}");
```
```
Find(20): 20
Find(99): -1
```

### 3.2 约束速查

| 约束 | 含义 | 不满足时的报错 |
|---|---|---|
| `where T : struct` | 不可空的值类型（`int`、`DateTime`、自定义 struct） | `CS0453` |
| `where T : class` | 引用类型（不可空） | `CS0452` |
| `where T : class?` | 引用类型，可空 | |
| `where T : notnull` | 不可空，值类型、引用类型都行（常用于字典的键） | `CS8714`（**只是警告**） |
| `where T : unmanaged` | 不含任何引用字段的值类型（可用于指针、`stackalloc`） | `CS8377` |
| `where T : new()` | 有 public 无参构造函数，可以 `new T()` | `CS0310` |
| `where T : Animal` | 是 `Animal` 或它的子类 | `CS0311` |
| `where T : IComparable<T>` | 实现了这个接口 | `CS0311` |
| `where T : U` | `T` 是另一个类型参数 `U` 或其子类 | |

和 Java 对比：Java 只有 `<T extends A & B>` 这一种上界约束；C# 多了值类型/引用类型/无参构造函数这些约束。

实测各约束不满足时的报错：

```csharp
var b1 = new ValBox<string>();        // where T : struct
var b2 = new RefBox<int>();           // where T : class
var b3 = new NewBox<NoCtor>();        // where T : new()，NoCtor 只有带参构造函数
var b4 = new BaseBox<string>();       // where T : Animal
var b5 = new CmpBox<object>();        // where T : IComparable<T>
var b6 = new UnmanagedBox<Holder>();  // where T : unmanaged，Holder 里有 string 字段
```
```
error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'ValBox<T>'
error CS0452: The type 'int' must be a reference type in order to use it as parameter 'T' in the generic type or method 'RefBox<T>'
error CS0310: 'NoCtor' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'NewBox<T>'
error CS0311: The type 'string' cannot be used as type parameter 'T' in the generic type or method 'BaseBox<T>'. There is no implicit reference conversion from 'string' to 'Animal'.
error CS0311: The type 'object' cannot be used as type parameter 'T' in the generic type or method 'CmpBox<T>'. There is no implicit reference conversion from 'object' to 'System.IComparable<object>'.
error CS8377: The type 'Holder' must be a non-nullable value type, along with all fields at any level of nesting, in order to use it as parameter 'T' in the generic type or method 'UnmanagedBox<T>'
```

`notnull` 比较特殊，违反时只是可空警告，照样能编译运行：

```csharp
var c1 = new Cache<string>();
var c2 = new Cache<int>();
var c3 = new Cache<string?>();
var c4 = new Cache<int?>();
class Cache<TKey> where TKey : notnull { }
```
```
warning CS8714: The type 'string?' cannot be used as type parameter 'TKey' in the generic type or method 'Cache<TKey>'. Nullability of type argument 'string?' doesn't match 'notnull' constraint.
warning CS8714: The type 'int?' cannot be used as type parameter 'TKey' in the generic type or method 'Cache<TKey>'. Nullability of type argument 'int?' doesn't match 'notnull' constraint.
```

### 3.3 约束的书写顺序 ⚠️

多个约束用逗号隔开，顺序有硬性规定：

```
where T : 主约束, 接口约束..., new()
          ↑ 只能一个         ↑ 必须最后
```

- **主约束**放第一个，最多一个：`class` / `class?` / `struct` / `unmanaged` / `notnull` / 某个基类。
- 接口约束放中间，可以多个。
- `new()` 放最后；`struct` 已经隐含无参构造，不能再写 `new()`。
- `class` 和基类不能同时写（基类本身就说明是引用类型）。
- 多个类型参数各写一个 `where`：`class Map<TKey, TValue> where TKey : notnull where TValue : class`。

实测各种写错的报错：

```csharp
class A<T> where T : new(), class { }                // new() 不在最后，class 不在最前
class B<T> where T : IComparable<T>, class { }       // class 不在最前
class C<T> where T : struct, new() { }               // struct 和 new() 一起写
class D<T> where T : Animal, class { }               // 基类和 class 一起写
class E<T> where T : Animal, Animal2 { }             // 两个基类
```
```
error CS0401: The new() constraint must be the last restrictive constraint specified
error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
error CS0451: The 'new()' constraint cannot be used with the 'struct' constraint
error CS0450: 'Animal': cannot specify both a constraint class and the 'class' or 'struct' constraint
error CS0406: The class type constraint 'Animal2' must come before any other constraints
```

正确写法：`where T : class, IEntity, new()`。

### 3.4 常用例子：用接口约束调用方法

```csharp
Console.WriteLine($"Max = {Max(3, 7)}, {Max("apple", "banana")}");

static T Max<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) >= 0 ? a : b;
```
```
Max = 7, banana
```

Java 写法是 `static <T extends Comparable<T>> T max(T a, T b)`。

## 4. default(T)

泛型代码里不知道 `T` 是值类型还是引用类型，不能直接 `return null`（见 3.1 的 `CS0403`）。用 `default`：引用类型得到 `null`，值类型得到"全 0"。

```csharp
Console.WriteLine($"default(int)      = {default(int)}");
Console.WriteLine($"default(bool)     = {default(bool)}");
Console.WriteLine($"default(DateTime) = {default(DateTime)}");
Console.WriteLine($"default(string)   = {default(string) ?? "null"}");
Console.WriteLine($"default(int?)     = {(default(int?) is null ? "null" : "有值")}");

Console.WriteLine($"FirstOrDefault(int[])   = {FirstOrDefault(new int[0])}");
Console.WriteLine($"FirstOrDefault(string[]) = {FirstOrDefault(new string[0]) ?? "null"}");

static T? FirstOrDefault<T>(T[] items)
{
    if (items.Length == 0) return default;   // 能推断类型时可以省略 (T)
    return items[0];
}
```
```
default(int)      = 0
default(bool)     = False
default(DateTime) = 1/1/0001 12:00:00 AM
default(string)   = null
default(int?)     = null
FirstOrDefault(int[])   = 0
FirstOrDefault(string[]) = null
```

⚠️ 对 `int` 来说，`default` 是 `0`，和"真的找到了 0"分不出来。LINQ 的 `FirstOrDefault()` 在 `List<int>` 上也是这样。需要区分"没找到"时，用 `int?`、`TryXxx(out T value)` 模式，或者返回 `bool`。

> 无约束的 `T?` 返回值：`T` 是引用类型时表示"可能为 null"；`T` 是 `int` 这样的值类型时，`T?` **仍然是 `int`**，不会变成 `int?`。所以上面 `FirstOrDefault(new int[0])` 返回的是 `0` 而不是 `null`。

## 5. 协变与逆变 🟡

### 5.1 问题：List<string> 不能赋给 List<object>

`string` 能赋给 `object`，但 `List<string>` 不能赋给 `List<object>`：

```csharp
List<string> names = ["a", "b"];
List<object> objs = names;
```
```
error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<string>' to 'System.Collections.Generic.List<object>'
```

原因：如果允许，就可以 `objs.Add(123)`，往一个字符串列表里塞了个 `int`，类型安全就破了。这一点和 Java 一样（Java 里 `List<Object> objs = new ArrayList<String>()` 也编译不过）。

但是**只读**的场景就是安全的：一个"只往外吐 `string`"的东西，当成"往外吐 `object`"的东西来用，不会出问题。协变和逆变就是用来表达这件事的。

### 5.2 协变 out T：T 只出现在返回值

```csharp
interface IProducer<out T>     // out：T 只能作为输出（返回值）
{
    T Get();
    // void Set(T item);       // ❌ 协变的 T 不能当参数
}

class Producer<T> : IProducer<T>
{
    private readonly T _value;
    public Producer(T value) => _value = value;
    public T Get() => _value;
}
```
```csharp
string name = "Owen";
IProducer<string> strProducer = new Producer<string>(name);
IProducer<object> objProducer = strProducer;     // ✅ 方向和 string → object 一致
Console.WriteLine($"协变：{objProducer.Get()}");

// 内置例子：IEnumerable<out T>
IEnumerable<object> objects = new List<string> { "a", "b" };
Console.WriteLine($"IEnumerable<object> = List<string>: {string.Join(", ", objects)}");
```
```
协变：Owen
IEnumerable<object> = List<string>: a, b
```

`List<T>` 不能协变，因为它有 `Add(T item)`，`T` 出现在参数里；`IEnumerable<T>` 只有"往外取"，所以标了 `out`。**需要"只读地接收一个子类型集合"时，参数类型写 `IEnumerable<T>`**，调用方就能传 `List<string>`、`string[]` 进来。

### 5.3 逆变 in T：T 只出现在参数

```csharp
interface IConsumer<in T>      // in：T 只能作为输入（参数）
{
    void Handle(T item);
    // T Get();                // ❌ 逆变的 T 不能当返回值
}

class Consumer<T> : IConsumer<T>
{
    public void Handle(T item) => Console.WriteLine($"逆变：{item}");
}
```
```csharp
IConsumer<object> objConsumer = new Consumer<object>();
IConsumer<string> strConsumer = objConsumer;     // ✅ 方向和 string → object 相反
strConsumer.Handle("能处理 object 的，当然也能处理 string");

// 内置例子：Action<in T>
Action<object> printAny = o => Console.WriteLine($"Action 逆变：{o}");
Action<string> printStr = printAny;
printStr("Owen");

// Func<in T, out TResult>：参数逆变、返回值协变
Func<object, string> f1 = o => $"[{o}]";
Func<string, object> f2 = f1;
Console.WriteLine($"Func：{f2("x")}");
```
```
逆变：能处理 object 的，当然也能处理 string
Action 逆变：Owen
Func：[x]
```

"能处理任何 `object` 的处理器"，当然可以拿去处理 `string`，反过来不行。

再一个实用例子：`IComparer<in T>`。一个比较 `object` 的比较器，可以直接给 `List<string>.Sort` 用：

```csharp
IComparer<object> cmp = Comparer<object>.Create((x, y) => x.ToString()!.Length.CompareTo(y.ToString()!.Length));
var list = new List<string> { "ccc", "a", "bb" };
list.Sort(cmp);                 // Sort 要的是 IComparer<string>
Console.WriteLine(string.Join(",", list));
```
```
a,bb,ccc
```

### 5.4 规则和限制

| | 协变 `out T` | 逆变 `in T` |
|---|---|---|
| `T` 可以出现在 | 返回值 | 参数 |
| 赋值方向 | `I<子类>` → `I<父类>` | `I<父类>` → `I<子类>` |
| 内置例子 | `IEnumerable<out T>`、`IReadOnlyList<out T>`、`Func<out TResult>` | `Action<in T>`、`IComparer<in T>`、`Func<in T, ...>` 的参数 |
| 一句话 | 生产者，只往外给 | 消费者，只往里收 |

限制（都实测过）：

1. **只能用在接口和委托上**，类不能写 `in`/`out`：

   ```
   error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
   ```

2. 编译器会检查 `T` 出现的位置，放错就报错：

   ```
   error CS1961: Invalid variance: The type parameter 'T' must be contravariantly valid on 'IBad<T>.Set(T)'. 'T' is covariant.
   error CS1961: Invalid variance: The type parameter 'T' must be covariantly valid on 'IBad2<T>.Get()'. 'T' is contravariant.
   ```

3. **只对引用类型有效**。`IEnumerable<int>` 不能赋给 `IEnumerable<object>`，因为 `int` → `object` 要装箱，内存布局不同：

   ```
   error CS0266: Cannot implicitly convert type 'System.Collections.Generic.IEnumerable<int>' to 'System.Collections.Generic.IEnumerable<object>'. An explicit conversion exists (are you missing a cast?)
   ```

### 5.5 ⚠️ 数组的协变是个历史坑

数组和 Java 一样是协变的，`string[]` 可以赋给 `object[]`，但写入时运行时才检查：

```csharp
string[] names = ["a", "b"];
object[] objs = names;          // 编译通过
Console.WriteLine($"objs[0] = {objs[0]}");
try
{
    objs[0] = 123;              // 编译通过，运行时报错
}
catch (Exception ex)
{
    Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
}
```
```
objs[0] = a
ArrayTypeMismatchException: Attempted to access an element as a type incompatible with the array.
```

对应 Java 的 `ArrayStoreException`。所以只读场景用 `IEnumerable<T>` / `IReadOnlyList<T>`，在编译期就保证安全。

### 5.6 和 Java 的 ? extends / ? super 对比

| | Java | C# |
|---|---|---|
| 在哪里声明 | **使用处**：每次写方法参数时写 `List<? extends Animal>` | **定义处**：接口定义时写一次 `IEnumerable<out T>`，用的时候什么都不用写 |
| 协变（只读） | `List<? extends Animal>` | `IEnumerable<Animal>`（因为接口本身是 `out T`） |
| 逆变（只写） | `Comparator<? super Cat>` | `IComparer<Cat>`（因为接口本身是 `in T`） |
| 口诀 | PECS：Producer Extends, Consumer Super | 生产者 `out`，消费者 `in` |
| 能用于类吗 | 能（`List<? extends T>`） | 不能，只有接口和委托 |

Java 是"用的人每次自己声明通配符"，C# 是"写接口的人声明一次，用的人直接享受"。所以 C# 代码里几乎看不到协变/逆变的语法，平时只要知道：**方法参数写 `IEnumerable<T>` 比写 `List<T>` 更通用**。

## 6. 小结

1. 泛型方法的 `<T>` 写在方法名后面；调用时类型参数一般能推断，不用写。
2. C# 泛型**不擦除**：运行时能拿到 `typeof(T)`，能 `obj is List<int>`、`new T[n]`；泛型类的静态字段每个 `T` 各一份。
3. `List<int>` 直接存 `int`，没有装箱，比 `ArrayList` 快一个数量级、分配少得多；不要再用非泛型集合。
4. 约束：主约束（`class`/`struct`/基类/`notnull`/`unmanaged`）放最前、接口放中间、`new()` 放最后；实现泛型接口时约束要重写一遍。
5. 泛型代码里不能 `return null`，用 `default`；注意值类型的 `default` 是 `0`，和"找到了 0"分不出来。
6. 协变 `out T`（只出不进）、逆变 `in T`（只进不出），只能用在接口和委托上，只对引用类型生效。
7. `List<T>` 不能协变是因为有 `Add`；只读的参数写成 `IEnumerable<T>` / `IReadOnlyList<T>`，调用方就能传子类型集合进来。

---

上一章：[继承、接口与多态 🟢](04-oop.md) · [目录](../README.md) · 下一章：[委托、Lambda 与事件 🟢](06-delegates-events.md)
