#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal delegate void CalculateDelegate(int a, int b);

    internal class Program
    {
        static void Main(string[] args)
        {
            // CalculateDelegate calculateDelegate = Add;
            CalculateDelegate calculateDelegate = new CalculateDelegate(Add);
            calculateDelegate += Sub;
            calculateDelegate.Invoke(2, 3);
        }

        static void Add(int a, int b)
        {
            Console.WriteLine($"{a} + {b} = {a + b}");
        }

        static void Sub(int a, int b)
        {
            Console.WriteLine($"{a} - {b} = {a - b}");
        }
    }
}