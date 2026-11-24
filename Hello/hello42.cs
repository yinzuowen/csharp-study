#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    public interface IRun
    {
        string Name { get; set; }

        void Run();
    }

    public class Animal : IRun
    {
        public string Name { get; set; } = "Animal";

        public void Run()
        {
            Console.WriteLine($"{Name} run");
        }
    }

    internal class Cat : Animal
    {
        public Cat()
        {
            Name = "Cat";
        }
    }

    internal class Dog : Animal
    {
        public Dog()
        {
            Name = "Dog";
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Animal animal = new Animal();
            animal.Run();

            Animal cat = new Cat();
            cat.Run();

            Animal dog = new Dog();
            dog.Run();
        }
    }
}