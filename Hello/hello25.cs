#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // Predicate 委托

    internal class Program
    {
        static void Main(string[] args)
        {
            Predicate<int> predicate = Check;
            int[] numbers = [1, 2, 3, 4, 5];
            foreach (var number in numbers)
            {
                Console.WriteLine($"{number} is {(predicate(number) ? "even" : "odd")}");
            }
        }

        static bool Check(int obj)
        {
            return obj % 2 == 0;
        }
    }
}