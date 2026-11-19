// 主题：两个全局"兜底"异常事件 —— AppDomain.UnhandledException vs TaskScheduler.UnobservedTaskException
using System.Runtime.CompilerServices;

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 任何线程上没被 catch 的异常 → 立刻触发；处理完进程照样崩溃（IsTerminating = true），只能记日志
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Console.WriteLine($"UnhandledException: {(e.ExceptionObject as Exception)?.Message}, IsTerminating = {e.IsTerminating}");
            };

            // 失败的 Task 从来没人看过它的异常 → 等这个 Task 被 GC 回收时才触发；不会让进程崩溃
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine($"UnobservedTaskException: {e.Exception.InnerException?.Message}");
                e.SetObserved();                     // 标记为"已处理"
            };

            FireAndForget();
            await Task.Delay(200);
            Console.WriteLine("Task 已经失败了，但没人 await 它，所以什么都没发生");

            GC.Collect();                            // 手动 GC，平时你不知道 GC 什么时候发生
            GC.WaitForPendingFinalizers();
            await Task.Delay(100);
            Console.WriteLine("GC 之后才触发了 UnobservedTaskException");

            // 取消下面两行的注释：普通线程里的未捕获异常 → UnhandledException → 进程崩溃，"main end" 不会打印
            // new Thread(() => throw new InvalidOperationException("thread boom")).Start();
            // await Task.Delay(500);

            Console.WriteLine("main end");
        }

        // 放在单独的方法里：Debug 模式下局部变量会活到方法结束，Task 不会被回收
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void FireAndForget()
        {
            Task.Run(() => throw new InvalidOperationException("task boom"));   // 不 await，异常没人看
        }
    }
}
