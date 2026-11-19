// 主题：ParameterizedThreadStart —— 给线程传参

// 只能传一个 object 参数，用的时候要转换

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(Run));
            thread.Start(DateTime.Now);
            thread.Join();

            // 现代写法：lambda 捕获变量，类型安全，不需要转换
            var now = DateTime.Now;
            var thread2 = new Thread(() => Console.WriteLine($"lambda: {now:HH:mm:ss}"));
            thread2.Start();
            thread2.Join();
        }

        // 委托定义是 ParameterizedThreadStart(object? obj)，所以参数要写 object?（否则 CS8622）
        static void Run(object? param)
        {
            if (param is DateTime time)
            {
                Console.WriteLine($"sub thread: {time:HH:mm:ss}");
            }
        }
    }
}
