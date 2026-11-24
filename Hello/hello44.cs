#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // sealed继承
    public sealed class MyHelper
    {

    }

    public class FileHelper : MyHelper
    {

    }

    public class Animal
    {
        public string Name { get; set; } = string.Empty;

        public virtual void Shout()
        {
            Console.WriteLine(Name);
        }
    }

    public class Cat : Animal
    {
        public override sealed void Shout()
        {
            Console.WriteLine("Meow");
        }
    }

    public class Tiger : Cat
    {
        public override void Shout()
        {
            Console.WriteLine("Tiger");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Animal animal = new Cat();
            animal.Shout();
        }
    }
}