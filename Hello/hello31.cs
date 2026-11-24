#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 元组

    internal class Program
    {
        static void Main(string[] args)
        {
            var obj1 = Tuple.Create(1, 2, 3);
            Console.WriteLine(obj1);

            var obj2 = Tuple.Create("Owen", 38);
            Console.WriteLine(obj2);

            (string, int) obj3 = ("Zoe", 10);
            Console.WriteLine(obj3);

            List<int> list = new List<int>() { 1, 2, 4, 8, 16 };
            var result = MinMax(list);
            Console.WriteLine($"min={result.min}, max={result.max}");
        }

        static (int min, int max) MinMax(List<int> list)
        {
            int min = int.MaxValue;
            int max = int.MinValue;

            foreach (var item in list)
            {
                if (item < min)
                {
                    min = item;
                }

                if (item > max)
                {
                    max = item;
                }
            }

            return (min, max);
        }
    }
}