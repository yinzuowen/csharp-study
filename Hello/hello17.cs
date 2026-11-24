#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal delegate void HelloWorldDelegate();
    internal delegate void HelloWorldDelegate2(string name);

    internal class Program
    {
        static void Main(string[] args)
        {
            HelloWorldDelegate helloWorldDelegate = Show;

            helloWorldDelegate();

            helloWorldDelegate.Invoke();

            Console.WriteLine("--------------------------");

            helloWorldDelegate += Show;

            helloWorldDelegate.Invoke();

            Console.WriteLine("--------------------------");

            HelloWorldDelegate2 helloWorldDelegate2 = Greet;

            helloWorldDelegate2.Invoke("Owen");
        }

        static void Show()
        {
            Console.WriteLine("Hello World");
        }

        static void Greet(string name)
        {
            Console.WriteLine($"Hello {name}");
        }
    }

}