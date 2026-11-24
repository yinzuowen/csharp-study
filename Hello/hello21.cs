#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // Action 委托

    internal class Program
    {
        static void Main(string[] args)
        {
            Action action1 = () =>
            {
                Console.WriteLine($"Hello World");
            };

            action1.Invoke();

            Action<string> action2 = Greet;

            action2.Invoke("Owen");

            Action<int, int> action3 = Add;

            action3(1, 2);
        }

        static void Greet(string name)
        {
            Console.WriteLine($"Hello {name}");
        }

        static void Add(int a, int b)
        {
            Console.WriteLine($"{a} + {b} = {a + b}");
        }
    }
}