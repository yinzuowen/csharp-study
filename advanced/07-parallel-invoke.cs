// 主题：Parallel.Invoke —— 同时执行多个方法；和串行执行的耗时对比
using System.Diagnostics;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            Stopwatch stopwatch = Stopwatch.StartNew();

            Method1();
            Method2();
            Method3();
            Console.WriteLine($"串行执行耗时：{stopwatch.ElapsedMilliseconds}ms");     // ≈ 0.5 + 1 + 1.5 = 3 秒

            stopwatch.Restart();

            // 同步的：会阻塞到全部完成才返回；调用它的线程自己也会参与执行
            Parallel.Invoke(Method1, Method2, Method3);
            Console.WriteLine($"并行执行耗时：{stopwatch.ElapsedMilliseconds}ms");     // ≈ 最慢的那个，1.5 秒

            Console.WriteLine("main thread end");
        }

        static void Method1()
        {
            Thread.Sleep(500);
            Console.WriteLine($"Method1 completed on thread {Environment.CurrentManagedThreadId}");
        }

        static void Method2()
        {
            Thread.Sleep(1000);
            Console.WriteLine($"Method2 completed on thread {Environment.CurrentManagedThreadId}");
        }

        static void Method3()
        {
            Thread.Sleep(1500);
            Console.WriteLine($"Method3 completed on thread {Environment.CurrentManagedThreadId}");
        }
    }
}
