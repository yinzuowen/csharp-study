#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 泛型结构体
    internal struct Point<T>
    {
        public T X;
        public T Y;
        public Point(T x, T y)
        {
            X = x;
            Y = y;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Point<int> point = new Point<int>(100, 200);
            Console.WriteLine(point);
        }
    }
}