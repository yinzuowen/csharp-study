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
            var array = new int[] { 1, 2, 3, 4, 5 };
            Func<int, int> square = x => x * x;
            Func<int, int> cube = x => x * x * x;
            var handlers = new List<Func<int, int>>();
            handlers.Add(square);
            handlers.Add(cube);
            foreach(var item in handlers)
            {
                var result = array.Select(item);
                Console.WriteLine(string.Join(", ", result));
            }
        }
    }
}