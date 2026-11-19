// 主题：async / await 基础 —— 方法写法、第一个 await 之前是同步执行的、await 和 Wait() 的区别
//
// async Task MethodAsync()            // 没有返回值
// {
//     await Task.Run(() => { /* 耗时操作 */ });
// }
//
// async Task<T> MethodAsync()         // 有返回值：方法里 return 的是 T，编译器包装成 Task<T>
// {
//     return await Task.Run(() => { /* 耗时操作 */ return default(T); });
// }

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ---------- 1. async 不等于"开线程"：第一个 await 之前在调用方线程上同步执行 ----------
            Console.WriteLine($"Main: 线程 {Environment.CurrentManagedThreadId}");
            Task<int> task = GetNumberAsync();
            Console.WriteLine("Main 继续（GetNumberAsync 遇到 await 之后才返回到这里）");
            int number = await task;
            Console.WriteLine($"number = {number}");

            Console.WriteLine("--------------------------");

            // ---------- 2. await vs Wait()：异常的形式不同 ----------
            try
            {
                await FailAsync();
            }
            catch (InvalidOperationException ex)          // await 抛出原始异常
            {
                Console.WriteLine($"await 捕获: {ex.GetType().Name}: {ex.Message}");
            }

            try
            {
                FailAsync().Wait();                      // 阻塞等待
            }
            catch (AggregateException ex)                // Wait() / Result 抛出包装后的 AggregateException
            {
                Console.WriteLine($"Wait() 捕获: {ex.GetType().Name} → {ex.InnerException?.Message}");
            }

            // 在 async 方法里应该用 await：Wait() / .Result 会阻塞线程（sync over async），
            // 在有 UI 线程的程序里还可能死锁
        }

        static async Task<int> GetNumberAsync()
        {
            Console.WriteLine($"await 前: 线程 {Environment.CurrentManagedThreadId}（和 Main 相同）");
            await Task.Delay(500);                       // 到这里才把控制权交还给调用方
            Console.WriteLine($"await 后: 线程 {Environment.CurrentManagedThreadId}（可能换了线程）");
            return 42;                                   // 返回 int，编译器把它放进 Task<int>
        }

        static async Task FailAsync()
        {
            await Task.Delay(100);
            throw new InvalidOperationException("boom");
        }
    }
}
