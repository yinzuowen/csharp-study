// 主题：等待任务 —— Wait()、Wait(int)、.Result、WaitAll、WhenAll

// ---------- Wait()：一直等到完成 ----------

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Task task = StartTicking(3);
            task.Wait();
            Console.WriteLine("Wait(): completed");

            Console.WriteLine("--------------------------");

            // ---------- Wait(int)：最多等多少毫秒，返回是否完成 ----------
            // 超时不会取消任务，只是不再等了；任务还在后台运行
            Task task2 = StartTicking(5);
            bool completed = task2.Wait(2500);
            Console.WriteLine(completed ? "Wait(2500): completed" : "Wait(2500): timeout, still running in background");
            task2.Wait();                               // 为了演示，等它真正结束

            Console.WriteLine("--------------------------");

            // ---------- .Result：读取结果时自动阻塞等待 ----------
            Task<int> t3 = Task.Run(() => { Thread.Sleep(500); return 1; });
            Task<int> t4 = Task.Run(() => { Thread.Sleep(500); return 2; });
            Console.WriteLine($".Result: {t3.Result + t4.Result}");   // 读了 Result，就不需要 WaitAll

            // ---------- WaitAll（阻塞） vs WhenAll（await，推荐） ----------
            Task.WaitAll(t3, t4);                       // 有异常时：等全部结束，把所有异常放进 AggregateException
            int[] all = await Task.WhenAll(t3, t4);     // 不阻塞线程，直接拿到结果数组
            Console.WriteLine($"WhenAll: {string.Join(", ", all)}");
        }

        static Task StartTicking(int count)
        {
            return Task.Factory.StartNew(state =>
            {
                if (state is not int n) return;     // state 是 object?：先判断再拆箱
                for (int i = 0; i < n; i++)
                {
                    Console.WriteLine($"  tick {i + 1}/{n} at {DateTime.Now:HH:mm:ss}");
                    Thread.Sleep(1000);
                }
            }, count);
        }
    }
}
