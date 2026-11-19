// 主题：ContinueWith —— 前一个任务完成后自动执行"延续任务"；以及用它代替委托的 BeginInvoke
//
// 委托的 BeginInvoke / EndInvoke（APM 模式）是 .NET Framework 的功能，现代 .NET 调用会抛 PlatformNotSupportedException：
//   job.BeginInvoke(num, callback, state)  →  Task.Run(() => job(num))
//   job.EndInvoke(ar)                      →  await task / task.Result
//   AsyncCallback                          →  task.ContinueWith(...) 或直接写在 await 后面

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            // ---------- 1. ContinueWith 基础：t 就是前一个任务 ----------
            Task<int> taskA = Task.Run(() =>
            {
                Console.WriteLine("taskA start");
                Thread.Sleep(1000);
                Console.WriteLine("taskA end");
                return 123;
            });

            Task taskB = taskA.ContinueWith(t =>
            {
                Console.WriteLine("taskB (continuation) start");
                Console.WriteLine($"taskA status: {t.Status}");     // RanToCompletion
                Console.WriteLine($"taskA result: {t.Result}");     // t 已完成，读 Result 不会阻塞
                Console.WriteLine("taskB end");
            });

            Console.WriteLine("main thread continues...");          // 主线程不用等，先往下执行
            taskB.Wait();                                           // 不等的话 Main 先结束，延续任务来不及执行

            Console.WriteLine("--------------------------");

            // ---------- 2. 延续任务也可以有返回值，并且可以串成链 ----------
            int final = await Task.Run(() => 2)
                .ContinueWith(t => t.Result * 10)                   // Task<int>
                .ContinueWith(t => t.Result + 1);                   // Task<int>
            Console.WriteLine($"链式 ContinueWith: {final}");        // 21

            Console.WriteLine("--------------------------");

            // ---------- 3. 按前一个任务的结果决定是否执行 ----------
            Task failed = Task.Run(() => throw new InvalidOperationException("boom"));
            Task onError = failed.ContinueWith(
                t => Console.WriteLine($"OnlyOnFaulted: {t.Exception?.InnerException?.Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
            await onError;

            Console.WriteLine("--------------------------");

            // ---------- 4. 代替 BeginInvoke / EndInvoke / AsyncCallback ----------
            Func<int, int> job = DoJob;
            int num = 500;

            Task<int> task = Task.Run(() => job(num));               // 相当于 BeginInvoke
            Task callback = task.ContinueWith(t =>                   // 相当于 AsyncCallback
                Console.WriteLine($"callback: completed, result={t.Result}"));

            int result = await task;                                 // 相当于 EndInvoke
            await callback;                                          // 回调也要等，否则可能来不及执行（CS4014）
            Console.WriteLine($"{num} * {num} = {result}");

            // 更直观的写法：await 后面的代码就相当于回调，日常开发很少需要 ContinueWith
            int result2 = await Task.Run(() => DoJob(20));
            Console.WriteLine($"await 之后：result2 = {result2}");

            Console.WriteLine("main thread end");
        }

        static int DoJob(int n)
        {
            Thread.Sleep(n);
            return n * n;
        }
    }
}
