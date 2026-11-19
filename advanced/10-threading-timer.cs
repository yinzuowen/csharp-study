// 主题：System.Threading.Timer —— 回调在线程池线程上执行；Timer 不能 Wait / await，要自己准备结束信号

namespace HelloWorld
{
    internal class Person
    {
        public int Balance { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            // ---------- 写法 1：ManualResetEventSlim + Wait()，会阻塞主线程 ----------
            Person person = new Person { Balance = 100 };
            using var done = new ManualResetEventSlim(false);

            // state = person；dueTime = 1000：1 秒后第一次执行；period = 3000：之后每 3 秒一次
            using (Timer timer = new Timer(state =>
            {
                if (state is Person p)
                {
                    Console.WriteLine($"[Wait]  balance = {p.Balance}");
                    p.Balance -= 20;
                    if (p.Balance == 0)
                    {
                        done.Set();                  // 通知主线程（用 Environment.Exit(0) 会直接杀掉进程）
                    }
                }
            }, person, 1000, 3000))
            {
                done.Wait();                         // 不等的话 Main 马上结束，回调一次都来不及执行
            }                                        // using：用完 Dispose，定时器停止

            Console.WriteLine("--------------------------");

            // ---------- 写法 2：TaskCompletionSource + await，不阻塞 ----------
            Person person2 = new Person { Balance = 60 };
            var tcs = new TaskCompletionSource();

            using (Timer timer2 = new Timer(_ =>
            {
                Console.WriteLine($"[await] balance = {person2.Balance}");
                person2.Balance -= 20;
                if (person2.Balance == 0)
                {
                    tcs.TrySetResult();              // 让 Task 完成
                }
            }, null, 500, 1000))
            {
                await tcs.Task;
            }

            Console.WriteLine("main thread end");
        }
    }
}
