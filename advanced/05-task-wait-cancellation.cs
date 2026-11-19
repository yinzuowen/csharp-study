// 主题：Wait(CancellationToken) / Wait(int, CancellationToken) —— 取消的是"等待"，不是任务

// ---------- Wait(token)：token 取消后抛 OperationCanceledException，任务继续跑 ----------

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var cts = new CancellationTokenSource(2000))
            {
                Task task = StartTicking(4);
                try
                {
                    task.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"不等了，但任务还在跑：Status = {task.Status}");
                }
                task.Wait();                             // 为了演示，等它真正结束
            }

            Console.WriteLine("--------------------------");

            // ---------- Wait(int, token)：超时 和 取消 哪个先到按哪个 ----------
            // 任务完成 → true；超时 → false；token 取消 → 抛异常
            using (var cts = new CancellationTokenSource(5000))
            {
                Task task = StartTicking(4);
                try
                {
                    bool completed = task.Wait(2000, cts.Token);   // 2 秒超时先到
                    Console.WriteLine(completed ? "completed" : "timeout（返回 false，不抛异常）");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("cancelled");
                }
                task.Wait();
            }

            // 要让任务真正停下来，必须把 token 传进任务内部检查（见 05-task-cancellation.cs）
        }

        static Task StartTicking(int count)
        {
            return Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    Console.WriteLine($"  tick {i + 1}/{count} at {DateTime.Now:HH:mm:ss}");
                    Thread.Sleep(1000);
                }
            });
        }
    }
}
