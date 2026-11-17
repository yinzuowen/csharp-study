// 主题：多态 —— 同一个父类变量，指向不同子类对象，调用同一个方法得到不同结果
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// 用父类类型统一处理所有子类，新增动物时这段代码不用改
List<Animal> animals = [new Cat(), new Dog(), new Pig()];

foreach (var animal in animals)
{
    animal.Run();                     // virtual：Pig 没重写，用父类默认的 Animal Run
    animal.Shout();                   // abstract：每个子类都必须实现
}

Console.WriteLine("--------------------------");

// 通过接口调用也是多态：IRun 绑定的是 Animal.Run，而它是 virtual 的，所以会走到子类
foreach (IRun runner in animals)
{
    runner.Run();
}

// 对比：如果子类用 new 隐藏而不是 override，父类变量和接口变量都只会调用 Animal 的版本（见 27-inheritance.cs）

interface IRun
{
    void Run();
}

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
