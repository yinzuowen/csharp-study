// 主题：Mutex —— 互斥锁，谁获取谁释放；比 lock 慢，进程内一般用 lock

namespace HelloWorld
{
    internal class Program
    {
        static int sum = 0;
        static Mutex mutex = new Mutex(false);

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>(5);

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Factory.StartNew((param) =>
                   {
                       mutex.WaitOne();                 // 放在 try 外面：没拿到锁就不能在 finally 里 ReleaseMutex
                       try
                       {
                           Console.WriteLine($"param = {param}");
                           Interlocked.Add(ref sum, (int)param!);
                       }
                       finally
                       {
                           mutex.ReleaseMutex();
                       }
                   }, i);

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"sum = {sum}");

            Console.WriteLine("main thread end");
        }
    }
}