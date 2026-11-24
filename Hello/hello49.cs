#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 多态
    internal interface IRun
    {
        void Run();
    }

    internal abstract class Animal : IRun
    {
        public virtual void Run()
        {
            Console.WriteLine("Animal Run");
        }

        public abstract void Shout();
    }

    internal class Cat : Animal
    {
        public override void Run()
        {
            Console.WriteLine("Cat Run");
        }

        public override void Shout()
        {
            Console.WriteLine("meow");
        }
    }

    internal class Dog : Animal
    {
        public override void Run()
        {
            Console.WriteLine("Dog Run");
        }

        public override void Shout()
        {
            Console.WriteLine("wang");
        }
    }

    internal class Pig : Animal
    {
        public override void Shout()
        {
            Console.WriteLine("eng");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Animal cat = new Cat();
            cat.Run();
            cat.Shout();

            Animal dog = new Dog();
            dog.Run();
            dog.Shout();

            Animal pig = new Pig();
            pig.Run();
            pig.Shout();
        }
    }
}