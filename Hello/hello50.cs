#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal interface IRun
    {
        void Run();
    }

    internal interface ISwim
    {
        void Swim();
    }

    interface IAnimal : IRun, ISwim
    {
        void Eat();
        void Sleep();
    }

    internal abstract class Animal : IAnimal
    {
        public virtual void Run()
        {
            Console.WriteLine("Animal Run");
        }

        public virtual void Swim()
        {
            Console.WriteLine("Animal Swim");
        }

        public virtual void Eat()
        {
            Console.WriteLine("Animal Eat");
        }

        public virtual void Sleep()
        {
            Console.WriteLine("Animal Sleep");
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

    internal class Program
    {
        static void Main(string[] args)
        {
            Animal cat = new Cat();
            cat.Run();
            cat.Swim();
            cat.Eat();
            cat.Sleep();
            cat.Shout();

            Animal dog = new Dog();
            dog.Run();
            dog.Swim();
            dog.Eat();
            dog.Sleep();
            dog.Shout();
        }
    }
}