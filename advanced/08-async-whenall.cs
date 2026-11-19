// 主题：在 async 方法里并发启动多个任务，用 await Task.WhenAll 收集结果
using System.Diagnostics;

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            Stopwatch stopwatch = Stopwatch.StartNew();
            int sum = await StartAsync();
            Console.WriteLine($"sum = {sum}, 耗时 {stopwatch.ElapsedMilliseconds}ms（≈ 最慢的 3 秒，而不是 1+2+3 = 6 秒）");

            Console.WriteLine("main thread end");
        }

        static async Task<int> StartAsync()
        {
            int[] numbers = [1, 2, 3];

            List<Task<int>> tasks = new List<Task<int>>();
            foreach (int n in numbers)
            {
                // 三个任务同时开始执行
                tasks.Add(Task.Factory.StartNew(DoSomething, n));   // 也可以写 Task.Run(() => DoSomething(n))
            }

            // WhenAll：不阻塞，等全部完成；结果数组的顺序和传入的任务顺序一致（不是完成顺序）
            int[] results = await Task.WhenAll(tasks);
            Console.WriteLine($"results = {string.Join(", ", results)}");

            return results.Sum();
        }

        // StartNew 的 state 参数是 object?，所以这里也要写 object?
        static int DoSomething(object? param)
        {
            if (param is int number)
            {
                Thread.Sleep(1000 * number);
                Console.WriteLine($"{number} * {number} = {number * number}");
                return number * number;
            }
            return 0;
        }
    }
}
