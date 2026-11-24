#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用


    // 接口
    public interface IRunOne
    {
        void Run();
    }

    public interface IRunTwo
    {
        void Run();
    }

    public class Animal : IRunOne, IRunTwo
    {
        public string Name { get; set; } = string.Empty;

        public virtual void Shout()
        {
            Console.WriteLine(Name);
        }

        public void Run()
        {
            Console.WriteLine("Run");
        }

        void IRunOne.Run()
        {
            Console.WriteLine("Run One");
        }

        void IRunTwo.Run()
        {
            Console.WriteLine("Run Two");
        }
    }

    public class Cat : Animal
    {
        public override void Shout()
        {
            Console.WriteLine("Meow");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Animal animal = new Cat();
            animal.Run();

            IRunOne iRunOne = animal as IRunOne;
            iRunOne.Run();

            IRunTwo iRunTwo = animal as IRunTwo;
            iRunTwo.Run();

            ((IRunOne)animal).Run();
            ((IRunTwo)animal).Run();
        }
    }
}