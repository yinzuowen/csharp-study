#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal delegate void Action(int number);

    internal class Program
    {
        static void Main(string[] args)
        {
            int[] numbers = [1, 2, 3, 4, 5];

            Action action = number =>
            {
                if (number % 2 == 0)
                {
                    Console.WriteLine($"{number}: even");
                }
                else
                {
                    Console.WriteLine($"{number}: odd");
                }
            };

            foreach (var number in numbers)
            {
                action(number);
            }
        }
    }
}