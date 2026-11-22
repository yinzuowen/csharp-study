// 主题：用两个 Semaphore 让两个线程交替执行：一个打印奇数，一个打印偶数

namespace HelloWorld
{
    internal class Program
    {
        static Semaphore semaphore1 = new Semaphore(1, 1);
        static Semaphore semaphore2 = new Semaphore(0, 1);
        static int num = 1;
        static int count = 5;

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            var task1 = Task.Run(() =>
               {
                   for (int i = 0; i < count; i++)
                   {

                       semaphore1.WaitOne();
                       Console.WriteLine($"odd = {num}");
                       Interlocked.Increment(ref num);
                       semaphore2.Release();
                   }
               });

            var task2 = Task.Run(() =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        semaphore2.WaitOne();
                        Console.WriteLine($"even = {num}");
                        Interlocked.Increment(ref num);
                        semaphore1.Release();
                    }
                });

            Task.WaitAll(task1, task2);

            Console.WriteLine("main thread end");
        }
    }
}