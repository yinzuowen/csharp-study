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
            Func<Func<int>, string> check = Check;
            string result = check.Invoke(() => 3);
            Console.WriteLine(result);
        }

        static string Check(Func<int> func)
        {
            int result = func();
            string parity = result % 2 == 0 ? "even" : "odd";
            return $"{result} is {parity}";
        }
    }
}