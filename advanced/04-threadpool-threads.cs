// 主题：线程池的两类线程 —— 工作线程 和 I/O 完成端口线程
//
// workerThreads：执行计算任务（Task.Run、QueueUserWorkItem、Parallel.For）
// completionPortThreads：处理异步 I/O 完成后的回调（来自 Windows 的 IOCP；Mac/Linux 上基本用不到）

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ThreadPool.GetMinThreads(out int minWorker, out int minIo);          // 可以"立即"创建的线程数，默认 = CPU 核数
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxIo);          // 上限
            ThreadPool.GetAvailableThreads(out int availWorker, out int availIo); // 上限 - 正在使用的

            Console.WriteLine($"CPU 核数: {Environment.ProcessorCount}");
            Console.WriteLine($"最小值: workerThreads={minWorker}, completionPortThreads={minIo}");
            Console.WriteLine($"最大值: workerThreads={maxWorker}, completionPortThreads={maxIo}");
            Console.WriteLine($"可用数: workerThreads={availWorker}, completionPortThreads={availIo}");

            // 超过最小值后，线程池大约每 0.5 秒才加一个线程。
            // 如果在线程池线程里大量阻塞（Thread.Sleep、.Result、同步 I/O），就会"线程池饥饿"，程序变慢。
            // 所以要用 async/await，不要阻塞线程池线程。
        }
    }
}
