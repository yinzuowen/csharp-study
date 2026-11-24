#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    public delegate int Multiple(int a, int b);

    internal class Program
    {
        static void Main(string[] args)
        {
            Multiple multiple = delegate (int a, int b)
            {
                return a * b;
            };

            Console.WriteLine($" 2 * 3 = {multiple(2, 3)}");
        }
    }
}