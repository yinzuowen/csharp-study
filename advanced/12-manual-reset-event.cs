// 主题：ManualResetEvent —— Set() 后门一直开着，所有等待的线程都放行，直到 Reset()

namespace HelloWorld
{
    // ManualReset: 调用 Set() 后门打开，变成有信号状态，而且会一直保持打开，直到有人显式调用 Reset() 才会关上。
    // 门开着的时候，所有正在等待的线程都会被放行。之后再来调用 WaitOne() 的线程也不会阻塞，会直接通过。
    internal class Program
    {
        static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>(5);

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Factory.StartNew((param) =>
                   {
                       manualResetEvent.WaitOne();
                       Console.WriteLine($"param = {param}");
                   }, i);

                tasks.Add(task);
            }

            manualResetEvent.Set();

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("main thread end");
        }
    }
}