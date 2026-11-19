// 主题：Task 里的异常 —— 不会让进程崩溃，而是保存在 Task 里，等你去"取"
//
// 处理方式：
// 1. 在任务方法内部捕获
// 2. 用 try/catch 包住 Wait() / Result（AggregateException）或 await（原始异常）
// 3. 在延续任务里处理前驱任务的异常

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ---------- 1. 在任务方法内部捕获 ----------
            await Task.Run(SafeMethod);

            // ---------- 2a. Wait() / Result：抛 AggregateException，原始异常在 InnerExceptions 里 ----------
            Task task = Task.Run(Method);
            try
            {
                task.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (Exception item in ex.InnerExceptions)
                {
                    Console.WriteLine($"2a. Wait 捕获: {item.Message}");
                }
            }

            // ---------- 2b. await：直接抛出原始异常（推荐） ----------
            try
            {
                await Task.Run(Method);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"2b. await 捕获: {ex.Message}");
            }

            // ---------- 3. 在延续任务里处理前驱任务的异常 ----------
            Task continuation = Task.Run(() => throw new Exception("前驱任务异常"))
                .ContinueWith(t =>
                {
                    // t.Exception 是 AggregateException?；Handle 里返回 true 表示"这个异常已处理"
                    t.Exception?.Handle(e =>
                    {
                        Console.WriteLine($"3. 延续任务处理: {e.Message}");
                        return true;
                    });
                }, TaskContinuationOptions.OnlyOnFaulted);   // 只在前驱任务失败时执行
            await continuation;                               // 不等的话 Main 先结束，看不到输出

            // 如果一个失败的 Task 从来没人 await / Wait / 读 Exception，异常就"无人处理"，
            // 只会在 GC 回收这个 Task 时触发 TaskScheduler.UnobservedTaskException（见 09-global-exception-handlers.cs）
        }

        static void SafeMethod()
        {
            try
            {
                throw new Exception("子线程异常 xxx");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"1. 方法内部捕获: {ex.Message}");
            }
        }

        static void Method()
        {
            throw new Exception("子线程异常 yyy");
        }
    }
}
