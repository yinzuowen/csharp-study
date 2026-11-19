// 主题：Parallel.For<TLocal> —— 线程本地小计 + 最后汇总，避免每次迭代都抢同一个变量
//
// Parallel.For<TLocal>(
//     int fromInclusive,                                  // 起始下标（包含）
//     int toExclusive,                                    // 结束下标（不包含）
//     Func<TLocal> localInit,                             // ① 每个线程开始时调用一次：小计的初始值
//     Func<int, ParallelLoopState, TLocal, TLocal> body,  // ② 每次迭代调用：(下标, 循环状态, 当前小计) → 新的小计
//     Action<TLocal> localFinally                         // ③ 每个线程结束时调用一次：把小计汇总到总数
// );

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            long sum = 0;

            Parallel.For<long>(1, 1001,
                () => 0L,
                (i, loop, subtotal) => subtotal + i,           // 只改本线程的小计，不需要加锁
                subtotal =>
                {
                    Console.WriteLine($"线程 {Environment.CurrentManagedThreadId} 的小计: {subtotal}");
                    // 多个线程同时写 sum，要用原子操作；ref：传变量本身（的地址），不是副本
                    Interlocked.Add(ref sum, subtotal);
                });

            Console.WriteLine($"1 + 2 + ... + 1000 = {sum}");   // 500500

            // 对比：
            //   Parallel.For(..., i => sum += i);                         ❌ 读-加-写不是原子的，会丢数据
            //   Parallel.For(..., i => Interlocked.Add(ref sum, i));      ⚠️ 正确，但 1000 次都在抢同一个变量
            //   线程本地小计 + localFinally                                ✅ 只同步"线程数"次

            // 简单求和用 PLINQ 一行就行，内部也是"分头算、最后汇总"
            long plinq = Enumerable.Range(1, 1000).AsParallel().Sum(x => (long)x);
            Console.WriteLine($"PLINQ: {plinq}");
        }
    }
}
