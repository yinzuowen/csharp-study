// 主题：接口 —— 实现多个接口、接口属性、显式接口实现、接口继承接口、给接口写扩展方法
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- 1. 接口里的属性只是"约定"，没有字段，不能写初始值 ----------
IRun runner = new Horse();
runner.Run();                         // Horse run

Console.WriteLine("--------------------------");

// ---------- 2. 两个接口有同名方法：一个 public 方法就能同时满足 ----------
var robot = new Robot();
IRunOne one = robot;
IRunTwo two = robot;
one.Run();
two.Run();

// ---------- 3. 显式接口实现：两个接口各自有不同实现 ----------
var robot2 = new Robot2();
((IRunOne)robot2).Run();              // Run One
((IRunTwo)robot2).Run();              // Run Two
// robot2.Run();                      // ❌ 显式实现只能通过接口类型调用

Console.WriteLine("--------------------------");

// ---------- 4. 接口继承接口：实现 IAnimal 就必须实现 IRun、ISwim 的全部成员 ----------
Animal cat = new Cat();
cat.Run();
cat.Swim();
cat.Eat();
ISwim swimmer = cat;                  // 可以当成任意一个父接口来用
swimmer.Swim();

Console.WriteLine("--------------------------");

// ---------- 5. 给接口写扩展方法：所有实现了 IInput 的类都自动拥有 Show() ----------
var reader = new Reader();
reader.Input("Hello World");
reader.Show();

// ===== 1 =====
interface IRun
{
    string Name { get; set; }         // 不能写 = string.Empty（CS8053），初始值放到实现类里
    void Run();                       // 接口成员默认 public
}

class Horse : IRun
{
    public string Name { get; set; } = "Horse";
    public void Run() => Console.WriteLine($"{Name} run");   // 实现时必须写 public（类成员默认 private）
}

// ===== 2 & 3 =====
interface IRunOne
{
    void Run();
}

interface IRunTwo
{
    void Run();
}

class Robot : IRunOne, IRunTwo
{
    public void Run() => Console.WriteLine("Robot Run（两个接口共用）");
}

class Robot2 : IRunOne, IRunTwo
{
    void IRunOne.Run() => Console.WriteLine("Run One");   // 方法名前加"接口名."，不能写 public
    void IRunTwo.Run() => Console.WriteLine("Run Two");
}

// ===== 4 =====
interface IMove
{
    void Run();
}

interface ISwim
{
    void Swim();
}

interface IAnimal : IMove, ISwim
{
    void Eat();
}

// 抽象类实现接口：可以给 virtual 默认实现，也可以继续留成 abstract 交给子类
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

// ===== 5 =====
interface IInput
{
    string Content { get; set; }
    void Input(string content);
}

class Reader : IInput
{
    public string Content { get; set; } = string.Empty;
    public void Input(string content) => Content = content;
}

// 抽象类实现接口时，接口的每个成员都要处理：要么实现，要么声明成 abstract
abstract class Inputer : IInput
{
    public abstract string Content { get; set; }   // 原 hello51 漏了这一行，会报 CS0535
    public abstract void Input(string content);
}

static class InputExtensions
{
    public static void Show(this IInput input) => Console.WriteLine(input.Content);
}
