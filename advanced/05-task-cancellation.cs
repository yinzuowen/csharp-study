// 主题：CancellationToken —— 协作式取消任务本身
//
// CancellationTokenSource（发令方）：Cancel()、CancelAfter(ms)
// CancellationToken（接收方）：IsCancellationRequested、ThrowIfCancellationRequested()

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            Task task = Task.Factory.StartNew(state =>
            {
                if (state is not int count) return;
                for (int i = 0; i < count; i++)
                {
                    token.ThrowIfCancellationRequested();   // 检查点：不检查就永远不会停
                    Console.WriteLine($"current time: {DateTime.Now:HH:mm:ss}");
                    Thread.Sleep(1000);
                }
            }, 10, token);                                   // 同一个 token 也传给 StartNew

            cts.CancelAfter(3000);                           // 3 秒后取消

            try
            {
                task.Wait();
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                // 传了 token → Canceled；不传 → Faulted（被当成普通异常）
                Console.WriteLine($"任务被取消，Status = {task.Status}");
            }

            Console.WriteLine("--------------------------");

            // 不想抛异常：用 IsCancellationRequested 安静地退出（状态是 RanToCompletion）
            using var cts2 = new CancellationTokenSource(1500);
            Task quiet = Task.Run(() =>
            {
                while (!cts2.Token.IsCancellationRequested)
                {
                    Console.WriteLine("working...");
                    Thread.Sleep(500);
                }
                Console.WriteLine("收到取消通知，退出");
            });
            quiet.Wait();
            Console.WriteLine($"Status = {quiet.Status}");

            Console.WriteLine("--------------------------");

            // 启动前就已取消：传了 token 的任务根本不会执行
            using var cts3 = new CancellationTokenSource();
            cts3.Cancel();
            Task skipped = Task.Run(() => Console.WriteLine("不会打印"), cts3.Token);
            try { skipped.Wait(); } catch (AggregateException) { }
            Console.WriteLine($"启动前已取消：Status = {skipped.Status}");

            // .NET 自带的异步方法大多接收 token
            using var cts4 = new CancellationTokenSource(500);
            try
            {
                await Task.Delay(5000, cts4.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Task.Delay 被取消");      // await 抛出的是 OperationCanceledException 本身，不是 AggregateException
            }
        }
    }
}
