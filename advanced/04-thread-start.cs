// 主题：Thread + ThreadStart —— 创建线程、前台/后台线程、Join

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Thread thread = new Thread(new ThreadStart(Run));   // 也可以简写成 new Thread(Run)
            // thread.IsBackground = true;                     // 后台线程：Main 结束时进程不等它，直接退出
            thread.Start();
            Console.WriteLine("main thread");                   // 通常比子线程先打印，但顺序不保证

            thread.Join();                                      // 等这个线程执行完再往下走
            Console.WriteLine("main thread end");
        }

        static void Run()
        {
            Thread.Sleep(500);
            Console.WriteLine($"sub thread, IsBackground = {Thread.CurrentThread.IsBackground}");
        }

        // 前台线程（默认）：进程会等它执行完才退出
        // 后台线程：进程退出时直接被终止；线程池里的线程全都是后台线程
    }
}
