#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // internal class Animal
    // {
    //     public virtual void Shout()
    //     {
    //         Console.WriteLine("shout");
    //     }
    // }

    internal abstract class Animal
    {
        public abstract void Shout();
    }

    internal class Cat : Animal
    {
        public override void Shout()
        {
            Console.WriteLine("meow");
        }
    }

    internal class Dog : Animal
    {
        public override void Shout()
        {
            Console.WriteLine("wang");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Animal animal = new Animal();
            // animal.Shout();

            Animal cat = new Cat();
            cat.Shout();

            Animal dog = new Dog();
            dog.Shout();
        }
    }
}