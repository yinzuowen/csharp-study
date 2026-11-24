# 第 4 章 继承、接口与多态 🟢

> 本章练习：[27-inheritance.cs](../basics/27-inheritance.cs)、[28-is-as.cs](../basics/28-is-as.cs)、[29-interfaces.cs](../basics/29-interfaces.cs)、[30-polymorphism.cs](../basics/30-polymorphism.cs)

> 从 Java 转过来写 C#，面向对象这块"看着一样、细节不一样"，最容易踩坑。你会遇到这些问题：
> 子类明明写了同名方法，用父类变量调用却还是父类的版本；编译器提示 `CS0108`、`CS0114`，不知道该加 `new` 还是 `override`；`obj as Cat` 之后一调用就 `NullReferenceException`；两个接口有同名方法不知道怎么分别实现；在子类里拿父类对象调 `protected` 方法却编译不过。
> 本章讲清楚：C# 的继承语法、`virtual`/`override` 和 Java 的区别、`new` 隐藏、抽象类与 `sealed`、类型判断与转换、接口（含默认实现和显式实现）、多态，以及访问修饰符的规则。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 继承的基本语法

### 1.1 单继承类、多实现接口

C# 和 Java 一样：**一个类只能继承一个父类，但可以实现多个接口**。区别是语法：Java 用 `extends` 和 `implements` 两个关键字，C# 统一用冒号 `:`，**父类写第一个，接口写在后面**。

```csharp
class EntityBase
{
    public int Id { get; set; }
}

class UserEntity : EntityBase          // Java：class UserEntity extends EntityBase
{
    public string Username { get; set; } = string.Empty;
}

class Robot : IRunOne, IRunTwo { ... } // Java：class Robot implements IRunOne, IRunTwo
class Cat : Animal, IRun, ISwim { ... } // 父类必须排第一个
```

```csharp
var user = new UserEntity { Id = 1, Username = "Owen" };
Console.WriteLine($"Id={user.Id}, Username={user.Username}");
```
```
Id=1, Username=Owen
```

写两个父类会直接报错：

```csharp
class X : Cat, Shape { }   // Cat 和 Shape 都是类
```
```
error CS1721: Class 'X' cannot have multiple base classes: 'Cat' and 'Shape'
```

| | Java | C# |
|---|---|---|
| 继承类 | `extends Base` | `: Base` |
| 实现接口 | `implements IA, IB` | `: IA, IB`（有父类时写在父类后面） |
| 调用父类构造函数 | `super(...)`，写在构造函数第一行 | `: base(...)`，写在构造函数签名后面 |
| 调用父类方法 | `super.foo()` | `base.Foo()` |
| 禁止继承 | `final class` | `sealed class` |
| 禁止重写 | `final` 方法 | `sealed override` 方法 |
| 所有类的根 | `java.lang.Object` | `System.Object`（别名 `object`） |

> 接口名按惯例以 `I` 开头（`IRun`、`IEnumerable`），一眼就能分清冒号后面哪个是类、哪个是接口。

### 1.2 base：调用父类构造函数和父类方法

**构造函数不会被继承**。父类没有无参构造函数时，子类必须用 `: base(...)` 显式调用，否则编译不过。父类构造函数先执行，子类后执行，和 Java 一样。

```csharp
var d = new Derived("Owen");
d.Show();

class BaseClass
{
    public string Name { get; }
    public BaseClass(string name)
    {
        Name = name;
        Console.WriteLine($"BaseClass 构造：{name}");
    }
    public virtual void Show() => Console.WriteLine($"BaseClass.Show: {Name}");
}

class Derived : BaseClass
{
    public Derived(string name) : base(name)    // Java：super(name);
    {
        Console.WriteLine("Derived 构造");
    }

    public override void Show()
    {
        base.Show();                              // 先执行父类逻辑
        Console.WriteLine("Derived.Show 追加的逻辑");
    }
}
```
```
BaseClass 构造：Owen
Derived 构造
BaseClass.Show: Owen
Derived.Show 追加的逻辑
```

泛型类继承也一样，练习里的例子：

```csharp
class SubContainer<T> : Container<T>
{
    public SubContainer(T value) : base(value) { }   // 父类只有带参构造函数，必须写 base(...)
}
```

## 2. virtual / override：和 Java 最大的区别 🟢

### 2.1 C# 的方法默认不是虚方法 ⚠️

| | Java | C# |
|---|---|---|
| 普通实例方法 | **默认是虚方法**，子类写同名方法就自动重写 | **默认不是虚方法**，子类写同名方法只是"隐藏" |
| 允许子类重写 | 什么都不用写 | 父类写 `virtual` |
| 子类重写 | 直接写（可加 `@Override` 注解） | **必须写 `override` 关键字** |
| 禁止重写 | `final` | 不写 `virtual`（默认就禁止）；或 `sealed override` |

也就是说，在 C# 里要实现多态，**父类 `virtual` 和子类 `override` 两边都要写**，缺一个都不是重写。

### 2.2 new 隐藏 vs override 重写：实测对比

同一个父类 `Base`，三个子类分别用 `new`、`override`、什么都不写：

```csharp
Base b1 = new HideChild();
Base b2 = new OverrideChild();
HideChild h = new HideChild();
b1.Hello();     // 变量类型 Base，对象类型 HideChild
h.Hello();      // 变量类型 HideChild
b2.Hello();     // 变量类型 Base，对象类型 OverrideChild

Base b3 = new Forgot();
b3.Hello();     // 子类既没写 new 也没写 override

class Base
{
    public virtual void Hello() => Console.WriteLine("Base.Hello");
    public void Plain() => Console.WriteLine("Base.Plain（非虚）");
}
class HideChild : Base
{
    public new void Hello() => Console.WriteLine("HideChild.Hello");          // new：隐藏
}
class OverrideChild : Base
{
    public override void Hello() => Console.WriteLine("OverrideChild.Hello"); // override：重写
}
class Forgot : Base
{
    public void Hello() => Console.WriteLine("Forgot.Hello");                 // 什么都没写
}
class Forgot2 : Base
{
    public void Plain() => Console.WriteLine("Forgot2.Plain");                // 父类方法不是 virtual
}
```

编译时的警告：

```
warning CS0108: 'Forgot2.Plain()' hides inherited member 'Base.Plain()'. Use the new keyword if hiding was intended.
warning CS0114: 'Forgot.Hello()' hides inherited member 'Base.Hello()'. To make the current member override that implementation, add the override keyword. Otherwise add the new keyword.
```

运行输出：

```
Base.Hello
HideChild.Hello
OverrideChild.Hello
Base.Hello
```

结论：

| 子类写法 | 父类变量调用（`Base b = new Child()`） | 子类变量调用 | 编译器提示 |
|---|---|---|---|
| `override` | **子类版本**（看对象实际类型，运行时决定） | 子类版本 | 无 |
| `new` | **父类版本**（看变量声明类型，编译时决定） | 子类版本 | 无 |
| 什么都不写，父类是 `virtual` | 父类版本（等同于 `new`） | 子类版本 | `CS0114` 警告 |
| 什么都不写，父类不是 `virtual` | 父类版本（等同于 `new`） | 子类版本 | `CS0108` 警告 |

- **只有 `override` 才是多态**。`new` 只是"子类里另起一个同名方法"，父类变量看不到它。
- ⚠️ 从 Java 过来的人最常见的错误：父类方法没写 `virtual`，子类写了同名方法，以为重写了，结果用父类变量调用一直走父类。编译器只给警告、不报错，**一定要看 `CS0108`/`CS0114` 警告**。
- `new` 在实际项目里很少用，基本只在"父类是别人的库、改不了，又非要用同名方法"时才用。看到 `CS0108`，正确做法通常是回去给父类加 `virtual`、子类改 `override`。

练习 [27-inheritance.cs](../basics/27-inheritance.cs) 里的版本：

```csharp
HideAnimal hide = new HideCat();
hide.Shout();                 // shout  ← 变量类型是 HideAnimal
((HideCat)hide).Shout();      // meow   ← 变量类型是 HideCat
```
```
shout
meow
```

### 2.3 什么能被 virtual

- 方法、属性、索引器、事件可以 `virtual`。
- `static` 方法、`private` 方法不能 `virtual`，字段也不能 `virtual`：

```
error CS0112: A static member cannot be marked as 'virtual'
error CS0621: 'Cat.P()': virtual or abstract members cannot be private
```

- `override` 时访问修饰符要和父类一致；父类里没有可重写的同名成员会报 `CS0115`：

```
error CS0115: 'Bad.Name()': no suitable method found to override
```

## 3. abstract 与 sealed

### 3.1 abstract vs virtual

```csharp
abstract class Animal
{
    public abstract void Shout();     // 没有方法体，子类必须 override

    public virtual void Run()         // 有默认实现，子类可以选择 override
    {
        Console.WriteLine("Animal Run");
    }
}

class Cat : Animal
{
    public sealed override void Shout() => Console.WriteLine("meow");
}

class Dog : Animal
{
    public override void Shout() => Console.WriteLine("wang");
    public override void Run() => Console.WriteLine("Dog Run");
}
```

```csharp
Animal cat = new Cat();
cat.Shout();
cat.Run();          // Cat 没重写 Run，用父类默认实现
Animal dog = new Dog();
dog.Shout();
dog.Run();
```
```
meow
Animal Run
wang
Dog Run
```

| | `abstract` 方法 | `virtual` 方法 |
|---|---|---|
| 有没有方法体 | 没有 | 有（默认实现） |
| 子类必须重写吗 | 必须，否则报 `CS0534` | 可选 |
| 能出现在哪 | 只能在 `abstract class` 里 | 任何非 `sealed` 的类 |
| 子类重写关键字 | `override` | `override` |

`abstract` 方法本身就是虚的，不用再写 `virtual`。和 Java 一样，抽象类不能 `new`。实测报错：

```csharp
var a = new Shape();                                  // Shape 是抽象类
class Animal { public abstract void Shout(); }        // 普通类里写 abstract 方法
class Cat : Shape { }                                 // 子类没实现抽象方法
```
```
error CS0144: Cannot create an instance of the abstract type or interface 'Shape'
error CS0513: 'Animal.Shout()' is abstract but it is contained in non-abstract type 'Animal'
error CS0534: 'Cat' does not implement inherited abstract member 'Shape.Area()'
```

### 3.2 sealed

`sealed` 有两种用法：

```csharp
sealed class MyHelper { }                 // 1. 类不能被继承（Java 的 final class），string 就是 sealed
class FileHelper : MyHelper { }           // ❌

class Lion : Cat
{
    public sealed override void Shout() { }   // 2. 重写，并禁止孙子类继续重写
}
class Cub : Lion
{
    public override void Shout() { }          // ❌
}
```
```
error CS0509: 'FileHelper': cannot derive from sealed type 'MyHelper'
error CS0239: 'Cub.Shout()': cannot override inherited member 'Lion.Shout()' because it is sealed
```

`sealed` 只能和 `override` 一起用在方法上。普通方法本来就不是虚的，不需要 `sealed`。

练习里 `Tiger : Cat`，而 `Cat.Shout` 是 `sealed override`，所以 `Tiger` 调用的还是 `Cat` 的版本：

```csharp
Animal tiger = new Tiger();
tiger.Shout();
```
```
meow
```

> 不打算被继承的类，可以直接加 `sealed`。这让意图更明确，JIT 也能对 `sealed` 类的方法调用做去虚化优化。

## 4. 类型判断与转换：is / as / 强制转换 🟢

### 4.1 is：判断类型

`is` 等于 Java 的 `instanceof`：对象是这个类型**或它的子类**就返回 `true`。`null is 任何类型` 都是 `false`。

```csharp
var animal = new Animal();
Console.WriteLine($"animal is Animal: {animal is Animal}");
Console.WriteLine($"animal is Cat: {animal is Cat}");
var cat = new Cat();
Console.WriteLine($"cat is Animal: {cat is Animal}");
Console.WriteLine($"cat is Cat: {cat is Cat}");

object? nothing = null;
Console.WriteLine($"null is Shape: {nothing is Shape}");
```
```
animal is Animal: True
animal is Cat: False
cat is Animal: True
cat is Cat: True
null is Shape: False
```

### 4.2 is 模式匹配：判断 + 转换一步完成（推荐）

Java 1.6 里要先 `instanceof` 再强转，写两遍类型名。C# 可以在 `is` 后面直接声明变量：

```csharp
object obj = new Cat();
if (obj is Animal a)          // 判断成立时，a 就是转换好的 Animal
{
    Console.WriteLine($"GetType().FullName: {a.GetType().FullName}");   // 实际类型
    a.Shout();                                                          // 多态
}

// 反过来写：不是就提前返回，后面 c 一定有值
if (obj is not Cat c) return;
c.Shout();
```
```
GetType().FullName: Cat
Meow
Meow
```

- `a.GetType()` 返回**对象的实际类型**（`Cat`），不是变量类型。
- `a` 的作用域：在 `if` 里能用；`is not` 写法下，`return` 之后的代码里能用。

### 4.3 as：失败返回 null

```csharp
Cat? cat2 = obj as Cat;                 // 成功
string? str = obj as string;            // 失败 → null，不抛异常
Console.WriteLine($"as string: {str ?? "null"}");
Console.WriteLine($"?. 安全调用：{str?.Length}");
```
```
as string: null
?. 安全调用：
```

⚠️ `as` 的结果可能是 `null`，要声明成可空类型 `Cat?`。如果写成不可空的 `Cat`，编译器会给可空警告；不处理直接调用，运行时就是空指针：

```csharp
object obj = "hello";
Cat cat = obj as Cat;       // 实际是 string，as 返回 null
cat.Shout();
```
```
warning CS8600: Converting null literal or possible null value to non-nullable type.
warning CS8602: Dereference of a possibly null reference.
Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
```

| 警告 | 含义 | 怎么改 |
|---|---|---|
| `CS8600` | 把可能为 null 的值赋给了不可空类型 | 变量声明成 `Cat?` |
| `CS8602` | 对可能为 null 的变量解引用（`.`） | 先判空，或用 `?.`，或直接改用 `is Cat c` |

> 可空引用类型（`string?`、`Cat?`）的完整说明见[第 9 章](09-modern-syntax.md)。现在只要记住：**这两个警告别忽略，它们就是在提前告诉你哪里会空指针。**

`as` 只能用于引用类型和可空值类型：

```csharp
object number = 42;
int? n = number as int?;      // obj as int 编译不过，要写 int?
Console.WriteLine($"as int?: {n}");
```
```
as int?: 42
```

### 4.4 强制转换：失败抛异常

```csharp
Cat cat3 = (Cat)obj;          // 确定类型时用
try
{
    _ = (string)obj;          // obj 实际是 Cat
}
catch (InvalidCastException ex)
{
    Console.WriteLine($"强转失败：{ex.GetType().Name}");
}
```
```
强转失败：InvalidCastException
```

### 4.5 怎么选

| 写法 | 失败时 | 适合 |
|---|---|---|
| `obj is Cat` | 返回 `false` | 只判断，不需要用转换后的对象 |
| `obj is Cat c` | 返回 `false`，`c` 不可用 | **大多数场景首选**：判断 + 转换 |
| `obj as Cat` | 返回 `null` | 老代码常见；新代码基本被 `is Cat c` 取代 |
| `(Cat)obj` | 抛 `InvalidCastException` | 确定一定是这个类型，不是就说明有 bug |

## 5. 接口 🟢

### 5.1 基本写法

```csharp
interface IRun
{
    string Name { get; set; }     // 接口属性：只是约定，没有字段
    void Run();                   // 接口成员默认 public，不用写
}

class Horse : IRun
{
    public string Name { get; set; } = "Horse";   // 初始值写在实现类里
    public void Run() => Console.WriteLine($"{Name} run");   // 实现时必须写 public
}
```
```
Horse run
```

- 接口成员默认 `public`；但**实现类里必须写 `public`**，因为类成员默认是 `private`。
- 实现接口方法**不用写 `override`**（`override` 只用于父类的 `virtual`/`abstract` 成员）。
- 少实现一个成员会报 `CS0535`：

```
error CS0535: 'R' does not implement interface member 'IA.Run()'
```

### 5.2 接口属性不能有初始值（CS8053）⚠️

接口属性只声明"有个 `get`/`set`"，背后没有字段可以存初始值：

```csharp
interface ILogger
{
    string Name { get; set; } = "x";    // ❌
}
```
```
error CS8053: Instance properties in interfaces cannot have initializers.
```

初始值放到实现类里：`public string Name { get; set; } = "x";`。

### 5.3 默认接口方法（C# 8+）

和 Java 8 的 `default` 方法类似，C# 接口方法可以带方法体，实现类不实现也行。C# 不需要写 `default` 关键字，直接写方法体：

```csharp
ILogger logger = new ConsoleLogger();
logger.Log("hello");
logger.LogError("出错了");          // ConsoleLogger 没实现 LogError，用接口默认实现
((ILogger)new FancyLogger()).LogError("被重写的默认实现");

interface ILogger
{
    void Log(string message);
    void LogError(string message) => Log($"[ERROR] {message}");   // 默认实现
}

class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine(message);
}

class FancyLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"Fancy: {message}");
    public void LogError(string message) => Console.WriteLine($"Fancy 自己的 LogError: {message}");
}
```
```
hello
[ERROR] 出错了
Fancy 自己的 LogError: 被重写的默认实现
```

⚠️ 和 Java 不同：**默认实现不会"继承"到类上**，只能通过接口类型的变量调用：

```csharp
var cl = new ConsoleLogger();   // 变量类型是 ConsoleLogger
cl.LogError("x");               // ❌
```
```
error CS1061: 'ConsoleLogger' does not contain a definition for 'LogError' and no accessible extension method 'LogError' accepting a first argument of type 'ConsoleLogger' could be found
```

默认接口方法主要用来**给已发布的接口加新方法而不破坏老的实现类**。日常业务代码里，能用抽象类或扩展方法就不必用它。

### 5.4 两个接口有同名方法：显式接口实现

**情况 1：一个 public 方法同时满足两个接口**

```csharp
class Robot : IRunOne, IRunTwo
{
    public void Run() => Console.WriteLine("Robot Run（两个接口共用）");
}

var robot = new Robot();
IRunOne one = robot;
IRunTwo two = robot;
one.Run();
two.Run();
```
```
Robot Run（两个接口共用）
Robot Run（两个接口共用）
```

**情况 2：两个接口要各自有不同实现 → 显式接口实现**

```csharp
class Robot2 : IRunOne, IRunTwo
{
    void IRunOne.Run() => Console.WriteLine("Run One");   // 方法名前加"接口名."，不能写 public
    void IRunTwo.Run() => Console.WriteLine("Run Two");
}

var robot2 = new Robot2();
((IRunOne)robot2).Run();
((IRunTwo)robot2).Run();
```
```
Run One
Run Two
```

显式实现的方法**只能通过接口类型调用**，用类变量调用会报错：

```csharp
var r = new Robot2();
r.Run();     // ❌
```
```
error CS1061: 'Robot2' does not contain a definition for 'Run' ...
```

Java 没有这个功能：两个接口有同签名的方法，Java 里只能写一个实现。C# 显式实现除了解决同名冲突，还常用来"把某些接口方法藏起来"，不出现在类的公开 API 里。

### 5.5 接口继承接口、抽象类实现接口

```csharp
interface IMove { void Run(); }
interface ISwim { void Swim(); }
interface IAnimal : IMove, ISwim       // 接口可以继承多个接口
{
    void Eat();
}

// 抽象类实现接口：每个成员要么实现（可以是 virtual），要么声明成 abstract 交给子类
abstract class Animal : IAnimal
{
    public virtual void Run() => Console.WriteLine("Animal Run");
    public virtual void Swim() => Console.WriteLine("Animal Swim");
    public abstract void Eat();
}

class Cat : Animal
{
    public override void Run() => Console.WriteLine("Cat Run");
    public override void Eat() => Console.WriteLine("Cat Eat");
}
```
```csharp
Animal cat = new Cat();
cat.Run();
cat.Swim();
cat.Eat();
ISwim swimmer = cat;       // 可以当成任意一个父接口用
swimmer.Swim();
```
```
Cat Run
Animal Swim
Cat Eat
Animal Swim
```

### 5.6 抽象类 vs 接口怎么选

| | 抽象类 | 接口 |
|---|---|---|
| 一个类能继承/实现几个 | 1 个 | 多个 |
| 字段 | 可以有 | 不能有实例字段 |
| 构造函数 | 可以有 | 不能有 |
| 成员访问修饰符 | 随意 | 默认 `public` |
| 表达的关系 | "是一个"（Cat 是 Animal），共享代码 | "能做什么"（能 Run、能比较），定义能力 |
| 常见用法 | 模板方法、共用基础逻辑 | 依赖注入、单元测试 mock、`IComparable`、`IDisposable` |

实际项目里**接口用得更多**：服务类基本都是"接口 + 实现类"（`IUserService` / `UserService`），方便依赖注入和测试替换（见[第 15 章](15-engineering.md)）。

## 6. 多态 🟢

**多态**：同一个父类（或接口）变量，指向不同子类对象，调用同一个方法得到不同结果。前提是父类方法是 `virtual`/`abstract`、子类用 `override`。

```csharp
List<Animal> animals = [new Cat(), new Dog(), new Pig()];
foreach (var animal in animals)
{
    animal.Run();      // virtual：Pig 没重写，用父类默认的
    animal.Shout();    // abstract：每个子类都必须实现
}

Console.WriteLine("--------------------------");

foreach (IRun runner in animals)   // 通过接口调用也是多态
{
    runner.Run();
}

interface IRun { void Run(); }

abstract class Animal : IRun
{
    public virtual void Run() => Console.WriteLine($"{GetType().Name}: Animal Run");
    public abstract void Shout();
}
class Cat : Animal
{
    public override void Run() => Console.WriteLine("Cat Run");
    public override void Shout() => Console.WriteLine("meow");
}
class Dog : Animal
{
    public override void Run() => Console.WriteLine("Dog Run");
    public override void Shout() => Console.WriteLine("wang");
}
class Pig : Animal
{
    public override void Shout() => Console.WriteLine("eng");
}
```
```
Cat Run
meow
Dog Run
wang
Pig: Animal Run
eng
--------------------------
Cat Run
Dog Run
Pig: Animal Run
```

注意 `Pig: Animal Run`：方法在父类里执行，但 `GetType()` 拿到的仍然是实际类型 `Pig`。

### 6.1 再看一个：计算面积

新增一种图形时，求总面积的代码不用改，这就是多态的价值：

```csharp
List<Shape> shapes = [new Circle(1), new Rect(2, 3), new Square(2)];
foreach (var s in shapes)
{
    Console.WriteLine($"{s.GetType().Name,-6} 面积 = {s.Area():F2}");
}
Console.WriteLine($"总面积 = {shapes.Sum(s => s.Area()):F2}");

abstract class Shape { public abstract double Area(); }
class Circle(double r) : Shape { public double R => r; public override double Area() => Math.PI * r * r; }
class Rect(double w, double h) : Shape { public double W => w; public double H => h; public override double Area() => w * h; }
class Square(double side) : Rect(side, side) { }
```
```
Circle 面积 = 3.14
Rect   面积 = 6.00
Square 面积 = 4.00
总面积 = 13.14
```

> `class Circle(double r) : Shape` 是 C# 12 的主构造函数写法，见[第 3 章](03-classes.md)。`Square(double side) : Rect(side, side)` 就相当于在构造函数里写 `: base(side, side)`。

### 6.2 new 隐藏会破坏多态，通过接口调用也一样

```csharp
IRun r = new HideDog();
r.Run();
((HideDog)r).Run();

interface IRun { void Run(); }
class Animal : IRun { public void Run() => Console.WriteLine("Animal Run"); }
class HideDog : Animal { public new void Run() => Console.WriteLine("HideDog Run"); }
```
```
Animal Run
HideDog Run
```

接口 `IRun.Run` 绑定的是 `Animal.Run`，`HideDog` 用 `new` 另起的方法接口看不到。

### 6.3 按类型分支：switch 模式匹配

有时候确实需要按具体类型做不同处理（比如对象来自外部、不能改它的类），用 `switch` 表达式比一串 `if ... is` 清楚：

```csharp
foreach (var s in shapes)
{
    string desc = s switch
    {
        Circle c => $"圆，半径 {c.R}",
        Square sq => $"正方形，边长 {sq.W}",      // Square 是 Rect 的子类，要写在 Rect 前面
        Rect { W: > 1 } rt => $"宽 > 1 的矩形 {rt.W}x{rt.H}",
        _ => "其他"
    };
    Console.WriteLine(desc);
}
```
```
圆，半径 1
宽 > 1 的矩形 2x3
正方形，边长 2
```

分支从上往下匹配，**子类要写在父类前面**。能用 `override` 解决的就别用类型分支，否则每加一个子类都要来改这里。模式匹配的完整用法见[第 9 章](09-modern-syntax.md)。

## 7. 访问修饰符与 protected 规则

### 7.1 修饰符速查

| 修饰符 | 谁能访问 | Java 对应 |
|---|---|---|
| `public` | 任何地方 | `public` |
| `private` | 只有本类 | `private` |
| `protected` | 本类和子类 | 接近 `protected`，但**不含同包访问** |
| `internal` | 同一个程序集（同一个项目 / dll） | 接近包访问（default），但范围是整个项目 |
| `protected internal` | 子类，**或**同一程序集里的任何代码 | 最接近 Java 的 `protected` |
| `private protected` | 同一程序集里的子类 | 无 |

默认值：

| 位置 | 不写修饰符时 | Java |
|---|---|---|
| 类的成员（字段、方法、属性） | `private` | 包访问 |
| 顶层类 | `internal` | 包访问 |
| 接口成员 | `public` | `public` |

⚠️ Java 习惯不写修饰符就是"包内可见"，C# 不写就是 `private`。实现接口方法忘写 `public` 会编译不过。

C# 也没有 C++ 的"公有继承/保护继承"：继承永远是公开的，修饰符只控制成员能被谁访问。

### 7.2 protected 的访问规则（CS0122 / CS1540）

```csharp
var c = new Child();
c.Eat();                              // ① 类外部访问 protected
c.Test(new Parent(), new Child());

class Parent
{
    protected void Eat() => Console.WriteLine("Eat");
    private void Secret() { }
}

class Child : Parent
{
    public void Test(Parent p, Child other)
    {
        Eat();          // ✅ 子类内部访问继承来的 protected 成员
        other.Eat();    // ✅ 通过 Child 类型的变量访问
        p.Eat();        // ② 通过 Parent 类型的变量访问
        Secret();       // ③ 访问父类的 private
    }
}
```
```
error CS1540: Cannot access protected member 'Parent.Eat()' via a qualifier of type 'Parent'; the qualifier must be of type 'Child' (or derived from it)
error CS0122: 'Parent.Secret()' is inaccessible due to its protection level
error CS0122: 'Parent.Eat()' is inaccessible due to its protection level
```

| 位置 | 结果 | 原因 |
|---|---|---|
| ① 类外部 `c.Eat()` | `CS0122` | `protected` 只对本类和子类内部开放 |
| ② 子类里 `p.Eat()`（`p` 是 `Parent`） | `CS1540` | 子类只能通过**自己（或自己的子类）类型**的引用访问 protected 成员 |
| ③ 子类里 `Secret()` | `CS0122` | `private` 子类也看不到 |

② 是最容易困惑的：`p` 可能其实是另一个子类 `OtherChild` 的对象，C# 不允许 `Child` 去动别的子类的 protected 成员。Java 里同包的代码可以访问 protected，所以 Java 程序员很少碰到这个错误，C# 里没有同包这一层。

练习里能编译通过的写法：

```csharp
var visible = new Child();
visible.Shout();         // public
// visible.Eat();        // ❌ protected
visible.Sleep();         // protected internal：同一程序集，可以
visible.CallEat();       // 子类内部再调 protected 的 Eat
```
```
public Shout
protected internal Sleep
protected Eat
```

## 8. 小结

1. C# 用 `:` 表示继承和实现接口，**只能继承一个类，可以实现多个接口**，父类写在第一个；调父类用 `base`。
2. **C# 方法默认不是虚方法**。要多态，父类写 `virtual`（或 `abstract`），子类写 `override`，两边都要写。
3. `new` 是隐藏不是重写：父类变量、接口变量都调不到子类版本。看到 `CS0108`/`CS0114` 警告，基本都是该改成 `virtual` + `override`。
4. `abstract` 没有方法体、子类必须重写；`virtual` 有默认实现、子类可选重写；`sealed` 禁止继承类或禁止继续重写。
5. 类型判断首选 `obj is Cat c`；`as` 失败返回 `null`，结果要声明成可空，`CS8600`/`CS8602` 警告不要忽略；强转失败抛 `InvalidCastException`。
6. 实现接口方法要写 `public`；接口属性不能有初始值（`CS8053`）；默认接口方法只能通过接口变量调用；两个接口同名方法要分别实现时用显式接口实现。
7. 类成员不写修饰符默认 `private`；子类只能通过自己类型的引用访问 `protected` 成员（`CS1540`）。

---

上一章：[类与对象 🟢](03-classes.md) · [目录](../README.md) · 下一章：[泛型 🟢](05-generics.md)
