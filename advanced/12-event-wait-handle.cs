// 主题：EventWaitHandle —— AutoResetEvent / ManualResetEvent 的基类，用 EventResetMode 选择两种模式

namespace HelloWorld
{
    internal class Program
    {
        // AutoReset: 每次 Set() 只放行一个等待中的线程，放行后事件会自动重置为无信号。
        // 其余线程会一直卡住，程序就挂起了。
        // static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        // ManualReset: 调用 Set() 后门打开，变成有信号状态，而且会一直保持打开，直到有人显式调用 Reset() 才会关上。
        // 门开着的时候，所有正在等待的线程都会被放行。之后再来调用 WaitOne() 的线程也不会阻塞，会直接通过。
        static EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>(5);

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Factory.StartNew((param) =>
                   {
                       waitHandle.WaitOne();
                       Console.WriteLine($"param = {param}");
                       // AutoReset 时，每个任务执行完后再调用一次 Set()，把下一个线程放进来
                       // waitHandle.Set();
                   }, i);

                tasks.Add(task);
            }

            Task.Delay(1000).ContinueWith(t => waitHandle.Set());

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("main thread end");
        }
    }
}