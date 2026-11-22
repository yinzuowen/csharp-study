// 主题：AutoResetEvent —— 每次 Set() 只放行一个等待的线程，放行后自动关门

namespace HelloWorld
{
    // AutoReset: 每次 Set() 只放行一个等待中的线程，放行后事件会自动重置为无信号。
    // 其余线程会一直卡住，程序就挂起了。
    // 要让 5 个任务都跑完，需要调用 5 次 Set()。另一种做法是让每个任务执行完后再调用一次 Set()，把下一个线程放进来，像接力一样。
    internal class Program
    {
        static AutoResetEvent autoResetEvent = new AutoResetEvent(false);
        static int count = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>(5);

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Factory.StartNew((param) =>
                   {
                       autoResetEvent.WaitOne();
                       Interlocked.Increment(ref count);
                       Console.WriteLine($"param = {param}");
                   }, i);

                tasks.Add(task);
            }

            while (Volatile.Read(ref count) < 5)
            {
                Thread.Sleep(100);
                autoResetEvent.Set();
            }

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("main thread end");
        }
    }
}