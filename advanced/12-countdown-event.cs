// 主题：CountdownEvent —— 计数到 0 才放行：等 N 个任务都准备好再开始（类似 Java 的 CountDownLatch）

namespace HelloWorld
{
    internal class Program
    {
        static int max = 5;
        static CountdownEvent countdownEvent = new CountdownEvent(max);

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            Task.Run(() =>
            {
                Thread.Sleep(500);
                Console.WriteLine("driver is ready");
                countdownEvent.Signal();
            });


            for (int i = 1; i < max; i++)
            {
                Task.Factory.StartNew((param) =>
                  {
                      Thread.Sleep(500);
                      Console.WriteLine($"passenger {param} is ready");
                      countdownEvent.Signal();
                  }, i);
            }

            countdownEvent.Wait();

            Console.WriteLine("start");

            Console.WriteLine("main thread end");
        }
    }
}