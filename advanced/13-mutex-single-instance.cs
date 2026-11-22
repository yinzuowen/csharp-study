// 主题：命名 Mutex —— 跨进程互斥，实现"程序只能运行一个实例"

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            // using 保持对 Mutex 的引用：写成 _ = new Mutex(...) 的话，对象可能被 GC 回收，锁就提前释放了
            using var mutex = new Mutex(true, "named", out bool createdNew);

            if (createdNew)
            {
                Console.WriteLine("Welcome，10 秒内在另一个终端再运行一次试试");
                Thread.Sleep(10000);
            }
            else
            {
                Console.WriteLine("Will exit");
                Thread.Sleep(1000);
                Environment.Exit(1);
            }

            Console.WriteLine("main thread end");
        }
    }
}