#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 泛型方法

    internal class Program
    {
        static void Main(string[] args)
        {
            int a = 1;
            int b = 2;
            Swap(ref a, ref b);
            Console.WriteLine($"a={a}, b={b}");

            string x = "x";
            string y = "y";
            Swap(ref x, ref y);
            Console.WriteLine($"x={x}, y={y}");
        }

        static void Swap<T>(ref T x, ref T y)
        {
            T temp = x;
            x = y;
            y = temp;
        }
    }
}