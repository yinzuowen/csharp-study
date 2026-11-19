// 主题：Parallel.For + ParallelLoopState —— Break() 和 Stop() 的区别
using System.Collections.Concurrent;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Parallel.For(from, to)：包含 from、不包含 to，所以这里是 1~40
            Run("Break", (i, state) => state.Break());
            Run("Stop", (i, state) => state.Stop());

            // Break()：比当前编号小的迭代保证执行完；比它大、还没开始的不再启动 —— 类似普通 for 里的 break
            // Stop()：全部尽快停下，不保证比它小的都执行过
            // 两者都不会打断已经开始执行的迭代，所以总会多执行一些编号更大的
        }

        static void Run(string name, Action<int, ParallelLoopState> stopAction)
        {
            var executed = new ConcurrentBag<int>();   // 多个线程同时 Add，要用线程安全的集合

            ParallelLoopResult result = Parallel.For(1, 41, (i, state) =>
            {
                if (i == 5)
                {
                    stopAction(i, state);
                    return;                            // Break/Stop 只是设标志，不会让当前方法退出，要自己 return
                }
                Thread.Sleep(100);
                executed.Add(i);
            });

            Console.WriteLine($"{name}: IsCompleted = {result.IsCompleted}");
            // LowestBreakIteration：调用 Break() 的那次迭代的编号（long?），用 Stop() 或没有中断时是 null
            Console.WriteLine($"{name}: LowestBreakIteration = {result.LowestBreakIteration?.ToString() ?? "null"}");
            Console.WriteLine($"{name}: 执行了 {string.Join(",", executed.OrderBy(x => x))}");
            Console.WriteLine();
        }
    }
}
