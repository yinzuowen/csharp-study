#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // Func 委托

    internal class Program
    {
        static void Main(string[] args)
        {
            Func<int, int, int> func1 = Add;
            func1.Invoke(2, 3);
        }

        static int Add(int a, int b)
        {
            Console.WriteLine($"{a} + {b} = {a + b}");
            return a + b;
        }
    }
}