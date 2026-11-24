# 第 10 章 反射与特性 🟡

> 本章练习：[01-reflection-members.cs](../advanced/01-reflection-members.cs)、[01-reflection-constructors.cs](../advanced/01-reflection-constructors.cs)、[01-reflection-methods.cs](../advanced/01-reflection-methods.cs)、[01-reflection-fields.cs](../advanced/01-reflection-fields.cs)、[01-reflection-properties.cs](../advanced/01-reflection-properties.cs)、[01-reflection-events.cs](../advanced/01-reflection-events.cs)、[01-reflection-interfaces.cs](../advanced/01-reflection-interfaces.cs)、[02-appdomain.cs](../advanced/02-appdomain.cs)、[02-assembly-load-context.cs](../advanced/02-assembly-load-context.cs)、[03-builtin-attributes.cs](../advanced/03-builtin-attributes.cs)、[03-custom-attribute.cs](../advanced/03-custom-attribute.cs)

> 业务代码里很少直接写反射，但你天天在用依赖反射的东西：JSON 序列化、ASP.NET Core 的依赖注入和模型绑定、EF Core、xUnit 找测试方法。写代码时你会遇到这些问题：
> 单文件程序第一行为什么要写 `#:property PublishAot=false`、`GetFields()` 为什么拿不到 private 字段、`MethodInfo.Invoke` 抛 `TargetParameterCountException`、`GetCustomAttribute` 抛 `AmbiguousMatchException`、照着老教程写 `AppDomain.CreateDomain` 直接报 `PlatformNotSupportedException`。
> 本章先讲反射 API 怎么用（Type、构造函数、方法、字段、属性、事件、接口），再讲特性（Attribute）的定义和读取，最后讲 AppDomain 的替代品、反射和 AOT 的冲突、反射的性能。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 反射是什么

反射 = 程序在**运行时**查看和操作类型的信息：有哪些字段、方法、属性，然后按名字去创建对象、调方法、读写字段（连 private 也行）。

Java 里你用过 `Class.forName`、`getDeclaredMethods`、`method.invoke`，C# 几乎一一对应：

| Java | C# |
|---|---|
| `Class<?>` | `Type` |
| `Person.class` | `typeof(Person)` |
| `obj.getClass()` | `obj.GetType()` |
| `Class.forName("com.x.Person")` | `Type.GetType("HelloWorld.Person")` |
| `Constructor` / `Method` / `Field` | `ConstructorInfo` / `MethodInfo` / `FieldInfo` |
| （没有属性、事件） | `PropertyInfo` / `EventInfo` |
| `setAccessible(true)` | 不需要，用 `BindingFlags.NonPublic` 就能拿到并读写 |
| 注解 `@interface` | 特性 `class XxxAttribute : Attribute` |

## 2. 获取 Type 🟢

三种方式：

| 写法 | 什么时候用 | 得不到时 |
|---|---|---|
| `typeof(Person)` | 编译时就知道类型 | — |
| `obj.GetType()` | 手上有一个对象，要它的**实际**类型 | — |
| `Type.GetType("HelloWorld.Person")` | 只有字符串（配置文件、插件名） | 返回 `null` |

```csharp
object obj = new Person();
Type t1 = typeof(Person);
Type t2 = obj.GetType();
Type? t3 = Type.GetType("HelloWorld.Person");
Type? t4 = Type.GetType("Person");          // 没写命名空间
Type? t5 = Type.GetType("System.String");
Console.WriteLine($"{t1.FullName} {t1 == t2} {t1 == t3} {t4 == null} {t5?.Name}");

object o2 = "abc";
Console.WriteLine($"{o2.GetType().Name} {typeof(object).Name}");
```
```
HelloWorld.Person True True True String
String Object
```

- 同一个类型只有一个 `Type` 对象，可以直接用 `==` 比较。
- `GetType()` 返回运行时的实际类型：变量声明成 `object`，装的是字符串，得到的就是 `String`。
- ⚠️ `Type.GetType` 的字符串必须带命名空间，写错不会抛异常，只是返回 `null`。别的程序集里的类型还要写程序集名（`"命名空间.类名, 程序集名"`）。

## 3. MemberInfo 家族与 BindingFlags

### 3.1 MemberInfo 家族

所有成员信息都继承自 `MemberInfo`：

```
MemberInfo（抽象基类）
├── FieldInfo        字段
├── PropertyInfo     属性
├── EventInfo        事件
├── MethodBase
│   ├── MethodInfo       方法
│   └── ConstructorInfo  构造函数
└── Type             类型本身（嵌套类型也是成员）
```

| 方法（复数返回数组，单数按名字找一个，找不到返回 null） | 返回 |
|---|---|
| `GetMembers()` / `GetMember(name)` | `MemberInfo[]` |
| `GetFields()` / `GetField(name)` | `FieldInfo` |
| `GetProperties()` / `GetProperty(name)` | `PropertyInfo` |
| `GetMethods()` / `GetMethod(name)` | `MethodInfo` |
| `GetConstructors()` / `GetConstructor(Type[])` | `ConstructorInfo` |
| `GetEvents()` / `GetEvent(name)` | `EventInfo` |
| `GetInterfaces()` | `Type[]` |

`GetMembers()` 默认只返回 **public** 成员，包括从 `object` 继承来的（[01-reflection-members.cs](../advanced/01-reflection-members.cs)）：

```csharp
internal class Person
{
    private int _age = 18;
    public event Action<Person>? OnCompleted;
    public string Name { get; set; } = string.Empty;
    public void Show() { ... }
}

foreach (MemberInfo member in typeof(Person).GetMembers())
    Console.WriteLine($"{member.MemberType,-12} {member.Name}");
```
```
Method       add_OnCompleted
Method       remove_OnCompleted
Method       get_Name
Method       set_Name
Method       Show
Method       GetType
Method       ToString
Method       Equals
Method       GetHashCode
Constructor  .ctor
Property     Name
Event        OnCompleted
```

从输出能看到编译器在背后做了什么：

- 属性 `Name` 编译后是 `get_Name` / `set_Name` 两个方法（Java 的 getter/setter，C# 帮你生成了）。
- 事件 `OnCompleted` 编译后是 `add_` / `remove_` 两个方法（对应 `+=` / `-=`）。
- 构造函数的名字永远是 `.ctor`。
- private 字段 `_age` 没出现。

### 3.2 BindingFlags 🟢

想拿 private 成员，或者只要自己声明的成员，就要传 `BindingFlags`：

| 标志 | 含义 |
|---|---|
| `Public` / `NonPublic` | 公开 / 非公开（private、protected、internal） |
| `Instance` / `Static` | 实例成员 / 静态成员 |
| `DeclaredOnly` | 只要本类声明的，不要继承来的 |

⚠️ **传了 BindingFlags，就必须同时指定 `Instance` 或 `Static`**，否则什么都查不到：

```csharp
Console.WriteLine($"只写 Public: {typeof(Person).GetMembers(BindingFlags.Public).Length}");
Console.WriteLine($"Public|Instance: {typeof(Person).GetMembers(BindingFlags.Public | BindingFlags.Instance).Length}");
Console.WriteLine($"Public|Static 字段: {string.Join(",", typeof(Person).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.Name))}");
```
```
只写 Public: 0
Public|Instance: 9
Public|Static 字段: Count
```

用 `Public | NonPublic | Instance | DeclaredOnly` 再按种类区分，就能看到全部"真实"成员：

```csharp
var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
foreach (MemberInfo member in typeof(Person).GetMembers(flags))
{
    string text = member switch
    {
        PropertyInfo p => $"属性 {p.Name}: {p.PropertyType.Name}",
        FieldInfo f => $"字段 {f.Name}: {f.FieldType.Name}",
        EventInfo e => $"事件 {e.Name}: {e.EventHandlerType?.Name}",
        ConstructorInfo c => $"构造 {c}",
        MethodInfo m when !m.IsSpecialName => $"方法 {m.Name}: {m.ReturnType.Name}",  // 排除 get_/set_/add_/remove_
        _ => ""
    };
    if (text != "") Console.WriteLine(text);
}
```
```
方法 Show: Void
构造 Void .ctor()
属性 Name: String
事件 OnCompleted: Action`1
字段 _age: Int32
字段 OnCompleted: Action`1
字段 <Name>k__BackingField: String
```

- 自动属性 `Name { get; set; }` 背后有一个编译器生成的私有字段 `<Name>k__BackingField`。
- 事件 `OnCompleted` 背后也有一个**同名**的私有委托字段，第 7 节会用到它。
- `Action`1` 是泛型类型在反射里的名字：反引号后面的数字是泛型参数个数。

## 4. 构造函数：创建对象 🟢

两种方式（[01-reflection-constructors.cs](../advanced/01-reflection-constructors.cs)）：

```csharp
Type type = typeof(Person);   // Person 有 (string) 和 (string, int) 两个构造函数

foreach (ConstructorInfo ctor in type.GetConstructors())
    Console.WriteLine($"{ctor.Name}: {ctor}");

// 方式 1：按参数类型找到构造函数，再 Invoke
ConstructorInfo? constructor = type.GetConstructor([typeof(string), typeof(int)]);
if (constructor?.Invoke(["Owen", 36]) is Person person)       // 参数必须放进数组
    Console.WriteLine($"ConstructorInfo.Invoke: {person.Name}, {person.Age}");

// 方式 2：Activator.CreateInstance，签名带 params
object? p1 = Activator.CreateInstance(type, "Zoe", 10);                    // 逐个传
object? p2 = Activator.CreateInstance(type, new object[] { "Zoe", 10 });   // 传数组也行
Console.WriteLine($"Activator: {(p1 as Person)?.Name}, {(p2 as Person)?.Name}");

Console.WriteLine($"constructor.GetType().Name = {constructor?.GetType().Name}");
```
```
.ctor: Void .ctor(System.String)
.ctor: Void .ctor(System.String, Int32)
ConstructorInfo.Invoke: Owen, 36
Activator: Zoe, Zoe
constructor.GetType().Name = RuntimeConstructorInfo
```

| | `ConstructorInfo.Invoke` | `Activator.CreateInstance` |
|---|---|---|
| 参数写法 | `object?[]` 数组，没有 `params` | `params object?[]`，逐个传或传数组都行 |
| 找构造函数 | 自己 `GetConstructor(Type[])` | 内部按实参自动匹配 |
| 适合 | 同一个构造函数要调很多次（先找好缓存起来） | 偶尔创建一次 |

- 编译时就知道类型的，用泛型 `new()` 约束：`static T Create<T>() where T : new() => new T();`，不需要反射，也不会返回 null（见 [03-custom-attribute.cs](../advanced/03-custom-attribute.cs)）。
- ⚠️ `constructor.GetType()` 得到的是**反射对象自己的类型** `RuntimeConstructorInfo`，不是构造函数的信息。第 6 节的 `FieldType` vs `GetType()` 是同一个坑。

## 5. 方法：MethodInfo.Invoke 🟢

```csharp
internal class Calculator
{
    public int Add(int x, int y) => x + y;
}

Type type = typeof(Calculator);
object? calculator = Activator.CreateInstance(type);
MethodInfo method = type.GetMethod("Add") ?? throw new InvalidOperationException("Add not found");

foreach (ParameterInfo parameter in method.GetParameters())
    Console.WriteLine($"{parameter.Name}: {parameter.ParameterType.Name}");
Console.WriteLine($"ReturnType: {method.ReturnType.Name}");

object? result = method.Invoke(calculator, [1, 2]);     // Invoke(对象, 参数数组)
Console.WriteLine($"{method.Name}: 1 + 2 = {result}");
if (result is int sum)                                  // 返回 object?，要转换
    Console.WriteLine($"sum * 10 = {sum * 10}");
```
```
x: Int32
y: Int32
ReturnType: Int32
Add: 1 + 2 = 3
sum * 10 = 30
```

要点：

- `Invoke(obj, args)`：第一个参数是"在哪个对象上调"，**静态方法传 `null`**；第二个参数是参数数组，**没有 `params`**。
- 无参方法：参数数组传 `null` 或 `[]` 都可以。参数个数不对抛 `TargetParameterCountException`：

```csharp
var hello = typeof(Person).GetMethod("Hello")!;          // public string Hello() => $"Hello, {Name}";
Console.WriteLine(hello.Invoke(p, null));
Console.WriteLine(hello.Invoke(p, []));
try { hello.Invoke(p, ["x"]); } catch (Exception e) { Console.WriteLine(e.GetType().Name + ": " + e.Message); }
```
```
Hello, Owen
Hello, Owen
TargetParameterCountException: Parameter count mismatch.
```

- ⚠️ 被调用的方法自己抛了异常，`Invoke` 会把它包一层 `TargetInvocationException`，真正的异常在 `InnerException` 里：

```csharp
static void Bad() => throw new InvalidOperationException("boom");

var bad = typeof(Program).GetMethod("Bad", BindingFlags.NonPublic | BindingFlags.Static)!;
try { bad.Invoke(null, null); }
catch (Exception e) { Console.WriteLine(e.GetType().Name + " -> " + e.InnerException?.GetType().Name); }
```
```
TargetInvocationException -> InvalidOperationException
```

- 反射调用和普通调用效果一样，方法里该触发的事件照样触发。事件是每个对象各自的，没订阅的对象不会触发：

```csharp
var person = new Person("Owen", 36);
person.OnCompleted += p => Console.WriteLine($"OnCompleted: {p.Name}");
MethodInfo? show = typeof(Person).GetMethod("Show");
show?.Invoke(person, null);                    // 触发
show?.Invoke(new Person("Zoe", 10), null);     // Zoe 没人订阅，不触发
```
```
name: Owen, age: 36
OnCompleted: Owen
name: Zoe, age: 10
```

## 6. 字段与属性

### 6.1 字段：FieldType vs GetType() ⚠️

```csharp
internal class Person
{
    public string name = string.Empty;
    private int age = 0;
    public override string ToString() => $"Person({name}, {age})";
}

var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
foreach (FieldInfo field in type.GetFields(flags))
    Console.WriteLine($"{field.Name}: FieldType={field.FieldType.Name}, GetType()={field.GetType().Name}");

var person = new Person();
FieldInfo? name = type.GetField("name");
FieldInfo? age = type.GetField("age", BindingFlags.NonPublic | BindingFlags.Instance);
name?.SetValue(person, "Owen");
age?.SetValue(person, 36);                      // 连 private 字段都能改
Console.WriteLine($"{name?.GetValue(person)}, {age?.GetValue(person)}");
Console.WriteLine(person);
```
```
name: FieldType=String, GetType()=RtFieldInfo
age: FieldType=Int32, GetType()=RtFieldInfo
Owen, 36
Person(Owen, 36)
```

| 想要什么 | 用什么 | 不要用 |
|---|---|---|
| 字段声明的类型 | `field.FieldType` | `field.GetType()`（得到 `RtFieldInfo`） |
| 属性的类型 | `property.PropertyType` | `property.GetType()` |
| 参数的类型 | `parameter.ParameterType` | `parameter.GetType()` |
| 方法返回值类型 | `method.ReturnType` | — |

- Java 里读 private 字段要先 `setAccessible(true)`；C# 不用，`BindingFlags.NonPublic` 拿到就能读写。
- 编译器不知道 `age` 会被反射读，会警告 CS0414"赋值了但从未使用"，练习里用 `#pragma warning disable` 关掉了。

### 6.2 属性

```csharp
internal class Person
{
    public string Name { get; set; } = string.Empty;
    public string Initial => Name.Length > 0 ? Name[..1] : "";   // 只读属性
    public int Age = 0;                                         // 这是字段，不是属性
}

foreach (PropertyInfo property in type.GetProperties())
    Console.WriteLine($"{property.MemberType}: {property.Name} - {property.PropertyType.Name}, CanWrite={property.CanWrite}");

PropertyInfo? nameProperty = type.GetProperty("Name");
nameProperty?.SetValue(person, "Owen");
Console.WriteLine($"Name = {nameProperty?.GetValue(person)}");
```
```
Property: Name - String, CanWrite=True
Property: Initial - String, CanWrite=False
Name = Owen
```

- `GetProperties()` 不包含字段，`Age` 没出现。
- 只读属性 `CanWrite = False`，对它 `SetValue` 实测抛 `ArgumentException: Property set method not found.`。写之前先判断 `CanWrite`。
- JSON 序列化器就是这样工作的：`GetProperties()` 遍历，`GetValue` 取值，拼成 JSON。

## 7. 事件：订阅与"强行触发"

```csharp
internal class Cat
{
    public event EventHandler<string>? OnMeow;
    public void Meow(string target) => OnMeow?.Invoke(this, target);   // 事件只能在类内部触发
}

EventInfo eventInfo = type.GetEvent("OnMeow")!;
EventHandler<string> handler = OnMeowCallback;      // 委托类型必须和事件类型一致

eventInfo.AddEventHandler(cat, handler);            // 相当于 cat.OnMeow += handler
cat.Meow("mouse");
eventInfo.RemoveEventHandler(cat, handler);         // 相当于 cat.OnMeow -= handler
cat.Meow("fish");                                   // 没人订阅，不输出

// 从外部强行触发（只用于练习）：通过同名的私有 backing field
eventInfo.AddEventHandler(cat, handler);
FieldInfo? field = type.GetField("OnMeow", BindingFlags.NonPublic | BindingFlags.Instance);
(field?.GetValue(cat) as EventHandler<string>)?.Invoke(cat, "bird (via backing field)");

static void OnMeowCallback(object? sender, string target) => Console.WriteLine($"{sender}: meow at {target}");
```
```
HelloWorld.Cat: meow at mouse
HelloWorld.Cat: meow at bird (via backing field)
```

- `EventInfo` 只提供 `AddEventHandler` / `RemoveEventHandler`，**没有"触发"方法**——事件的设计就是只能在类内部触发。
- 简写事件（`public event Xxx? OnMeow;`）背后有一个**同名的私有委托字段**，用 `GetField(..., NonPublic | Instance)` 拿到它，取出委托直接 `Invoke`，就从外部触发了。这破坏了封装，只用来理解原理。
- ⚠️ 直接 `handler.Invoke(cat, "mouse")` 是调用委托本身，不经过事件，证明不了订阅成功。

## 8. 接口查询

```csharp
internal class Cat : IRun, ISwim { ... }

foreach (Type item in type.GetInterfaces())
    Console.WriteLine($"{item.MemberType}, {item.FullName}");

Console.WriteLine($"Cat 实现了 IRun: {typeof(IRun).IsAssignableFrom(type)}");
Console.WriteLine($"Cat 实现了 IDisposable: {typeof(IDisposable).IsAssignableFrom(type)}");
```
```
TypeInfo, HelloWorld.IRun
TypeInfo, HelloWorld.ISwim
Cat 实现了 IRun: True
Cat 实现了 IDisposable: False
```

- `A.IsAssignableFrom(B)`：B 类型的值能不能赋给 A 类型的变量。方向容易写反：**接口写在前面**。等价于 Java 的 `IRun.class.isAssignableFrom(Cat.class)`。
- 手上有对象时直接用 `obj is IRun`，不需要反射。
- 插件系统、依赖注入容器扫描程序集时，就是用它找"所有实现了某接口的类"。

## 9. 特性（Attribute）🟢

特性 = 贴在类、方法、属性、参数上的**元数据标签**，对应 Java 的注解。特性本身什么都不做，要么编译器认识它（编译时生效），要么有代码用反射读它（运行时生效）。

### 9.1 常用内置特性

| 特性 | 作用 | 谁来处理 |
|---|---|---|
| `[Obsolete("消息", false)]` | 标记过时：`false` 警告 CS0618，`true` 编译错误 | 编译器 |
| `[Conditional("DEBUG")]` | 没定义 `DEBUG` 符号时，**调用它的语句**被编译器删掉 | 编译器 |
| `[CallerMemberName]` 等 | 编译器自动把调用方信息填进参数 | 编译器 |
| `[Serializable]`、`[JsonPropertyName("x")]` | 序列化相关 | 序列化库（反射） |
| `[Fact]`、`[Theory]` | 标记测试方法（第 15 章） | xUnit（反射） |
| `[AttributeUsage]` | 规定自定义特性能用在哪里 | 编译器 |
| `[MethodImpl(NoInlining)]` | 不允许 JIT 内联这个方法 | JIT |

`Obsolete` 和 `Conditional`（[03-builtin-attributes.cs](../advanced/03-builtin-attributes.cs)）：

```csharp
static void Main(string[] args)
{
    #pragma warning disable CS0618            // 演示用：关闭"调用了过时方法"的警告
    OldShow("Hello World");
    #pragma warning restore CS0618
    NewShow("Hello World");

    Log("这一行只在 Debug 编译时输出");

    #if DEBUG
    Console.WriteLine("#if DEBUG：效果类似，但删的是这段代码本身");
    #endif
}

[Obsolete("请改用 NewShow", false)]
static void OldShow(string message) => Console.WriteLine($"old: {message}");
static void NewShow(string message) => Console.WriteLine($"new: {message}");

[Conditional("DEBUG")]                                   // 只能用在返回 void 的方法上
static void Log(string message) => Console.WriteLine($"[DEBUG] {message}");
```

`dotnet run 03-builtin-attributes.cs`（默认 Debug）：

```
old: Hello World
new: Hello World
[DEBUG] 这一行只在 Debug 编译时输出
#if DEBUG：效果类似，但删的是这段代码本身
```

`dotnet run -c Release 03-builtin-attributes.cs`：

```
old: Hello World
new: Hello World
```

`[Conditional]` 删的是**调用处**，`Log` 方法本身还在程序集里；`#if` 删的是被包住的那段代码。`Debug.WriteLine`、`Debug.Assert` 就是用 `[Conditional("DEBUG")]` 实现的，所以 Release 版本里它们没有任何开销。

调用方信息特性：写日志、实现 `INotifyPropertyChanged` 时常用，编译时就填好了，没有运行时开销：

```csharp
static void Log(string msg,
    [CallerMemberName] string member = "",
    [CallerFilePath] string file = "",
    [CallerLineNumber] int line = 0,
    [CallerArgumentExpression(nameof(msg))] string expr = "")
    => Console.WriteLine($"{msg} from {member} {Path.GetFileName(file)}:{line} expr={expr}");

Log("hi");    // 在 Main 里调用
```
```
hi from Main typeget.cs:58 expr="hi"
```

这些参数必须有默认值，调用时不要自己传。

### 9.2 自定义特性

```csharp
// 特性就是继承 Attribute 的类，类名按惯例以 Attribute 结尾，使用时可以省略后缀
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]   // 只能用在类上，可以标记多次
internal class AuthorAttribute : Attribute
{
    public string Name { get; }
    public string Message { get; set; }

    // 参数默认值必须是编译时常量：string.Empty 是 static readonly，不能用，要写 ""
    public AuthorAttribute(string name, string message = "")
    {
        Name = name;
        Message = message;
    }
}

[Author("Owen", Message = "first version")]   // 构造函数参数 + 命名属性
[Author("Zoe")]
internal class Cat { public void Show() => Console.WriteLine("Meow"); }

internal class Dog { public void Show() => Console.WriteLine("Wang"); }
```

读取：

```csharp
static T CreateInstance<T>() where T : new()
{
    var attribute = typeof(T).GetCustomAttributes<AuthorAttribute>().FirstOrDefault();
    Console.WriteLine(attribute != null ? $"[{typeof(T).Name}] 第一个 Author: {attribute.Name}" : $"[{typeof(T).Name}] 没有 Author");
    return new T();
}

CreateInstance<Cat>().Show();
CreateInstance<Dog>().Show();
foreach (var attr in typeof(Cat).GetCustomAttributes<AuthorAttribute>())
    Console.WriteLine($"Author: {attr.Name}, {attr.Message}");
```
```
[Cat] 第一个 Author: Owen
Meow
[Dog] 没有 Author
Wang
Author: Owen, first version
Author: Zoe,
```

特性参数的规则：

| 写法 | 对应 | 必填 |
|---|---|---|
| `[Author("Owen")]` | 构造函数参数（位置参数） | 是 |
| `[Author("Owen", Message = "x")]` | 可写的 public 属性或字段（命名参数） | 否 |

参数值只能是编译时常量：数字、字符串、`bool`、枚举、`typeof(X)`，以及它们的数组。不能传 `new Person()`、`DateTime.Now`。

### 9.3 AttributeUsage

| 参数 | 含义 | 默认 |
|---|---|---|
| `AttributeTargets` | 能贴在哪里：`Class`、`Method`、`Property`、`Field`、`Parameter`……可以用 `\|` 组合 | `All` |
| `AllowMultiple` | 同一个位置能不能贴多次 | `false` |
| `Inherited` | 子类能不能继承父类上的特性 | `true` |

对应 Java 的 `@Target` 和 `@Repeatable`。注意 Java 注解要写 `@Retention(RUNTIME)` 才能在运行时反射读到，**C# 的特性总是保留在元数据里**，没有这个问题。

### 9.4 GetCustomAttribute 抛 AmbiguousMatchException ⚠️

`AllowMultiple = true` 的特性贴了多次，用**单数**的 `GetCustomAttribute` 读会抛异常：

```csharp
[Author("Owen")]
[Author("Zoe")]
internal class Cat { }

try { typeof(Cat).GetCustomAttribute<AuthorAttribute>(); }
catch (Exception e) { Console.WriteLine(e.GetType().FullName + ": " + e.Message); }
Console.WriteLine(string.Join(",", typeof(Cat).GetCustomAttributes<AuthorAttribute>().Select(a => a.Name)));
Console.WriteLine(typeof(Cat).IsDefined(typeof(AuthorAttribute)));
```
```
System.Reflection.AmbiguousMatchException: Multiple custom attributes of the same type 'HelloWorld.AuthorAttribute' found.
Owen,Zoe
True
```

| 方法 | 没贴 | 贴了 1 次 | 贴了多次 |
|---|---|---|---|
| `GetCustomAttribute<T>()`（单数） | `null` | 返回它 | ❌ 抛 `AmbiguousMatchException` |
| `GetCustomAttributes<T>()`（复数） | 空序列 | 1 个元素 | 全部 |
| `IsDefined(typeof(T))` | `false` | `true` | `true` |

规则：**`AllowMultiple = true` 的特性一律用复数版**；只判断有没有贴，用 `IsDefined`，它不创建特性对象，更快。`GetCustomAttribute<T>()` 是 `System.Reflection` 命名空间里的扩展方法，记得 `using System.Reflection;`。

## 10. AppDomain 与 AssemblyLoadContext ⚪

老教程（.NET Framework）会教 `AppDomain.CreateDomain` 创建隔离区来加载、卸载插件。在现代 .NET 里**每个进程只有一个 AppDomain**，创建新的直接抛异常：

```csharp
#pragma warning disable SYSLIB0024
try { AppDomain.CreateDomain("x"); }
catch (Exception e) { Console.WriteLine(e.GetType().FullName + ": " + e.Message); }
#pragma warning restore SYSLIB0024
```
```
System.PlatformNotSupportedException: Secondary AppDomains are not supported on this platform.
```

编译时也有警告 SYSLIB0024 提醒你。`AppDomain.CurrentDomain` 还能用（[02-appdomain.cs](../advanced/02-appdomain.cs)）：

```csharp
AppDomain current = AppDomain.CurrentDomain;
Console.WriteLine($"FriendlyName:  {current.FriendlyName}");
foreach (var assembly in current.GetAssemblies())
    Console.WriteLine($"  {assembly.GetName().Name}");
current.UnhandledException += (sender, e) => Console.WriteLine($"未处理异常: {e.ExceptionObject}");
```
```
FriendlyName:  02-appdomain
已加载的程序集：
  System.Private.CoreLib
  02-appdomain
  System.Runtime
  System.Console
  System.Threading
  Microsoft.Win32.Primitives
  System.Runtime.InteropServices
```

现在还常用的是 `UnhandledException` 全局异常事件（第 13 章）。

需要"加载程序集、用完再卸载"（插件热更新），用 **`AssemblyLoadContext`**（[02-assembly-load-context.cs](../advanced/02-assembly-load-context.cs)）：

```csharp
static void Main(string[] args)
{
    Console.WriteLine($"默认上下文: {AssemblyLoadContext.Default.Name}");
    WeakReference weak = CreateAndUnload();

    // Unload() 只是发起卸载，真正释放要等 GC；还有对象引用着它就卸载不掉
    for (int i = 0; weak.IsAlive && i < 10; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    Console.WriteLine($"卸载后还活着吗: {weak.IsAlive}");
}

// 必须放在单独的方法里：Debug 模式下局部变量会活到方法结束，GC 回收不了
[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference CreateAndUnload()
{
    var context = new AssemblyLoadContext("myContext", isCollectible: true);   // isCollectible 才能卸载
    Console.WriteLine($"新建: {context.Name}, 可卸载: {context.IsCollectible}");
    // 实际使用：var asm = context.LoadFromAssemblyPath("/path/plugin.dll");
    context.Unload();
    return new WeakReference(context);
}
```
```
默认上下文: Default
新建: myContext, 可卸载: True
卸载后还活着吗: False
```

| 需求 | .NET Framework | 现代 .NET |
|---|---|---|
| 加载插件程序集 | `AppDomain.CreateDomain` + 加载 | `new AssemblyLoadContext(name, isCollectible: true)` + `LoadFromAssemblyPath` |
| 卸载 | `AppDomain.Unload` | `context.Unload()`，等 GC 回收 |
| 真正的隔离（崩溃不影响主程序、权限隔离） | AppDomain | **独立进程** |

`AssemblyLoadContext` 只隔离"加载了哪个版本的程序集"，不隔离异常和内存：插件崩了，整个进程照样崩。

## 11. 反射与 AOT / 裁剪 🟡

### 11.1 为什么单文件程序要写 `PublishAot=false`

用 `dotnet run xxx.cs` 运行的单文件程序（file-based app）**默认开启 `PublishAot=true`**，目标是发布成 Native AOT：直接编译成机器码，启动快、体积小、不需要装 .NET 运行时。

AOT 发布时会做**裁剪**（trimming）：分析代码，把用不到的类型和成员删掉。问题是反射是"运行时按名字找"，编译器静态分析不出来你要用哪些成员，可能把它们删掉，运行时就找不到了。所以开着 AOT 时，编译器的裁剪分析器会对"它看不懂的反射"发出 IL 警告。

实测：去掉第一行 `#:property PublishAot=false`，编译这段代码：

```csharp
static object? Create<T>() => Activator.CreateInstance(typeof(T));
```
```
warning IL2087: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicParameterlessConstructor' in call to 'System.Activator.CreateInstance(Type)'. The generic parameter 'T' of 'HelloWorld.Program.Create<T>()' does not have matching annotations. ...
```

再比如对泛型参数取属性：

```csharp
static void Dump<T>(T obj)
{
    foreach (var p in typeof(T).GetProperties())
        Console.WriteLine($"{p.Name} = {p.GetValue(obj)}");
}
```
```
warning IL2090: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties' in call to 'System.Type.GetProperties()'. The generic parameter 'T' of 'HelloWorld.Program.Dump<T>(T)' does not have matching annotations. ...
```

意思是：编译器不知道 `T` 会是什么，没法保证 `T` 的构造函数 / 属性在裁剪后还在。

- `dotnet run` 时只是警告，程序照常运行（上面第一个例子实测输出 `Cat`）；**真正 AOT 发布后才可能运行出错**。
- `typeof(Person).GetFields()` 这种类型写死的反射，分析器看得懂，没有警告。

### 11.2 怎么处理

| 做法 | 说明 |
|---|---|
| 第一行写 `#:property PublishAot=false` | 学习、写工具脚本时最省事，本仓库的练习都这样写 |
| 给泛型参数加标注 | `static object? Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()`，告诉裁剪器保留它（实测加上后 IL2087 消失） |
| 用 `new()` 约束代替反射 | `where T : new()` 再 `new T()`，编译时就确定了 |
| 用源生成器代替反射 | 比如 `System.Text.Json` 的 `JsonSerializerContext`：编译时生成序列化代码 |

普通的 `.csproj` 项目（`dotnet new console`）默认没有开 AOT，不会有这些警告。

## 12. 反射的性能，什么时候用

调用同一个 `Add` 方法 1000 万次（Release，第二轮计时）：

```csharp
var calc = new Calculator();
MethodInfo method = typeof(Calculator).GetMethod("Add")!;
var func = method.CreateDelegate<Func<int, int, int>>(calc);    // 把 MethodInfo 变成强类型委托

for (int i = 0; i < N; i++) sum += calc.Add(i, 1);               // 直接调用
for (int i = 0; i < N; i++) sum += (int)method.Invoke(calc, [i, 1])!;   // 反射调用
for (int i = 0; i < N; i++) sum += func(i, 1);                   // 委托调用
```
```
调用 10,000,000 次：直接调用 7ms，MethodInfo.Invoke 271ms，CreateDelegate 后调用 32ms
```

（具体数字和机器有关，看量级。）

- `MethodInfo.Invoke` 比直接调用慢**几十倍**：每次都要分配参数数组、把 `int` 装箱成 `object`、检查参数类型。
- `GetMethod`、`GetProperties` 这些"查找"本身也有开销，**查到的 `MethodInfo` 要缓存起来**，不要每次调用都查一遍。
- 要反复调用时，用 `CreateDelegate` 转成强类型委托，接近直接调用的速度。
- 绝对值上，一次反射调用也就几十纳秒。偶尔调一次（启动时扫描程序集、读配置）完全不用在意。

什么时候用反射：

| 场景 | 用不用 |
|---|---|
| 写框架、库：序列化、依赖注入、ORM、测试框架、插件加载 | ✅ 这是反射的主场 |
| 根据配置里的类名创建对象 | ✅ 可以，但先想想能不能用字典 + 工厂代替 |
| 写单元测试时读写 private 成员 | ⚠️ 能用，但通常说明设计有问题 |
| 业务代码里按字符串调方法 | ❌ 用接口、委托、`switch` |
| 编译时就知道类型 | ❌ 直接写代码，或者泛型 + `new()` 约束 |

## 13. 小结

1. 拿 `Type` 三种方式：`typeof(X)`、`obj.GetType()`、`Type.GetType("命名空间.类名")`（找不到返回 null）。
2. 传 `BindingFlags` 时必须同时写 `Instance` 或 `Static`；要 private 成员加 `NonPublic`。
3. 创建对象：`Activator.CreateInstance(type, 参数...)` 带 `params`；`ConstructorInfo.Invoke` 和 `MethodInfo.Invoke` 都要传数组，无参传 `null` 或 `[]`，静态方法的对象传 `null`。
4. 字段、属性、参数的类型用 `FieldType` / `PropertyType` / `ParameterType`，`GetType()` 拿到的是反射对象自己的类型。
5. 特性就是继承 `Attribute` 的类，用 `AttributeUsage` 规定用法；`AllowMultiple = true` 时用复数的 `GetCustomAttributes`，单数版会抛 `AmbiguousMatchException`。
6. `AppDomain.CreateDomain` 在现代 .NET 抛 `PlatformNotSupportedException`；可卸载加载用 `AssemblyLoadContext`，真正隔离用独立进程。
7. 单文件程序默认开 AOT，反射会触发 IL2087 / IL2090 等裁剪警告，学习时写 `#:property PublishAot=false`。
8. 反射调用比直接调用慢几十倍：缓存 `MethodInfo`，热点路径用 `CreateDelegate`；业务代码能不用就不用。

---

上一章：[现代 C# 语法 🟢](09-modern-syntax.md) · [目录](../README.md) · 下一章：[线程与线程池 🟡](11-threads.md)
