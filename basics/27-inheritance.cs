// 主题：继承 —— 单继承、base、new 隐藏 vs virtual/override、abstract、访问修饰符、sealed、泛型类继承
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- 1. 基本继承：子类自动拥有父类的成员（C# 只能继承一个类，用 : 表示） ----------
var user = new UserEntity { Id = 1, Username = "Owen" };
Console.WriteLine($"Id={user.Id}, Username={user.Username}");

Console.WriteLine("--------------------------");

// ---------- 2. new（隐藏）：看变量的声明类型，编译时决定 ----------
HideAnimal hide = new HideCat();
hide.Shout();                         // shout  ← 变量类型是 HideAnimal
((HideCat)hide).Shout();              // meow   ← 变量类型是 HideCat

// ---------- 3. virtual + override（重写）：看对象的实际类型，运行时决定 —— 这才是多态 ----------
Animal cat = new Cat();
cat.Shout();                          // meow
cat.Run();                            // Animal Run  ← Cat 没重写 virtual 的 Run，用父类默认实现
Animal dog = new Dog();
dog.Shout();                          // wang
dog.Run();                            // Dog Run
// var animal = new Animal();         // ❌ 抽象类不能 new

Console.WriteLine("--------------------------");

// ---------- 4. 访问修饰符 ----------
var visible = new Child();
visible.Shout();                      // public：哪里都能访问
// visible.Eat();                     // ❌ protected：只有本类和子类内部能访问
visible.Sleep();                      // protected internal：子类，或同一程序集里的任何代码
visible.CallEat();                    // 子类内部可以调用 protected 的 Eat

Console.WriteLine("--------------------------");

// ---------- 5. 泛型类继承：构造函数不会被继承，用 : base(...) 调用父类构造函数 ----------
Container<int> container = new SubContainer<int>(123);
Console.WriteLine($"Value={container.Value}");

// ---------- 6. sealed ----------
Animal tiger = new Tiger();
tiger.Shout();                        // meow：Cat 的 Shout 是 sealed override，Tiger 不能再重写

class EntityBase
{
    public int Id { get; set; }
}

class UserEntity : EntityBase
{
    public string Username { get; set; } = string.Empty;
}

// ===== new 隐藏 =====
class HideAnimal
{
    public void Shout() => Console.WriteLine("shout");
}

class HideCat : HideAnimal
{
    public new void Shout() => Console.WriteLine("meow");   // new：故意隐藏父类同名方法（不写 new 会有 CS0108 警告）
}

// ===== virtual / abstract / override =====
abstract class Animal
{
    public abstract void Shout();     // abstract：没有方法体，子类必须 override，只能出现在抽象类里

    public virtual void Run()         // virtual：有默认实现，子类可以选择 override
    {
        Console.WriteLine("Animal Run");
    }
}

class Cat : Animal
{
    // sealed override：重写，并且禁止孙子类继续重写
    public sealed override void Shout() => Console.WriteLine("meow");
}

class Dog : Animal
{
    public override void Shout() => Console.WriteLine("wang");
    public override void Run() => Console.WriteLine("Dog Run");
}

class Tiger : Cat
{
    // public override void Shout() { }   // ❌ CS0239：Cat.Shout 是 sealed
}

// ===== 访问修饰符 =====
// C# 没有 C++ 的"公开继承/保护继承"，继承永远是公开的；修饰符只控制成员能被谁访问
class Parent
{
    public void Shout() => Console.WriteLine("public Shout");
    protected void Eat() => Console.WriteLine("protected Eat");
    protected internal void Sleep() => Console.WriteLine("protected internal Sleep");
    // 不写修饰符的类成员默认是 private（Java 默认是包内可见）
}

class Child : Parent
{
    public void CallEat() => Eat();   // 子类内部可以访问 protected；Eat、this.Eat、base.Eat 在这里都一样
}

// ===== 泛型类继承 =====
class Container<T>
{
    public T Value { get; set; }

    public Container(T value)
    {
        Value = value;
    }
}

class SubContainer<T> : Container<T>
{
    public SubContainer(T value) : base(value)   // 父类没有无参构造函数，必须写 base(...)（Java 是 super(...)）
    {
    }
}

// ===== sealed 类：不能被继承（例如 string 就是 sealed） =====
sealed class MyHelper
{
}

// class FileHelper : MyHelper { }    // ❌ CS0509：不能继承 sealed 类
