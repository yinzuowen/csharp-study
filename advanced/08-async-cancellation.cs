// 主题：在 async 方法里取消 —— 把 token 传给 Task.Delay 等异步方法，取消立即生效

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(3000);                    // 3 秒后取消（代替"另开一个任务 Sleep 3 秒再 Cancel"）

            // ---------- 写法 1：手动检查 token ----------
            try
            {
                await LoopWithCheckAsync(cts.Token);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine($"写法 1：{ex.Message}");
            }

            Console.WriteLine("--------------------------");

            // ---------- 写法 2：token 传给 Task.Delay（推荐） ----------
            using var cts2 = new CancellationTokenSource(3000);
            try
            {
                await LoopWithDelayTokenAsync(cts2.Token);
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine($"写法 2：{ex.Message}");
            }

            // 这里直接 await 就行，不需要 Task.Run(async () => await ...) 再包一层

            Console.WriteLine("main thread end");
        }

        static async Task LoopWithCheckAsync(CancellationToken token)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");
                await Task.Delay(1000);               // 不传 token：取消后要等这 1 秒结束才能发现
                // 取消可能发生在 Delay 期间，所以 Delay 之后要再检查一次；
                // ThrowIfCancellationRequested 自己会判断，外面不用再包 if (token.IsCancellationRequested)
                token.ThrowIfCancellationRequested();
            }
        }

        static async Task LoopWithDelayTokenAsync(CancellationToken token)
        {
            while (true)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");
                await Task.Delay(1000, token);        // 一旦取消，Delay 立即结束并抛 OperationCanceledException
            }
        }
    }
}
