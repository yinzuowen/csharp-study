// 主题：Semaphore —— 信号量：最多允许 N 个线程同时进入；Release() 返回释放前的计数

namespace HelloWorld
{
    internal class Program
    {
        static int initialCount = 0;
        static int maxCount = 5;
        static int sum = 0;
        static Semaphore semaphore = new Semaphore(initialCount, maxCount);

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>(5);

            for (int i = 0; i < maxCount; i++)
            {
                var task = Task.Factory.StartNew((param) =>
                   {
                       semaphore.WaitOne();
                       Console.WriteLine($"param = {param}");
                       Interlocked.Add(ref sum, (int)param!);
                   }, i);

                tasks.Add(task);
            }

            int prevCount = -1;

            prevCount = semaphore.Release();
            Console.WriteLine($"prevCount = {prevCount}");

            prevCount = semaphore.Release();
            Console.WriteLine($"prevCount = {prevCount}");

            prevCount = semaphore.Release();
            Console.WriteLine($"prevCount = {prevCount}");

            prevCount = semaphore.Release();
            Console.WriteLine($"prevCount = {prevCount}");

            prevCount = semaphore.Release();
            Console.WriteLine($"prevCount = {prevCount}");

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"sum = {sum}");

            Console.WriteLine("main thread end");
        }
    }
}