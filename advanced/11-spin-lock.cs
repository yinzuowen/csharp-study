// 主题：SpinLock —— 拿不到锁时原地循环（自旋）而不是睡眠；只适合锁住时间极短的代码

namespace HelloWorld
{
    // 自旋锁（SpinLock）
    // 未获得互斥锁的线程会进入睡眠状态，而未获得SpinLock的线程将一直处于循环状态
    internal class Program
    {
        static int sum = 0;
        static SpinLock spinLock = new SpinLock();   // SpinLock 是 struct：不能加 readonly，也不能复制，否则锁的是副本

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Factory.StartNew(param =>
                {
                    bool lockTaken = false;
                    int number = (int)param!;
                    try
                    {
                        spinLock.TryEnter(5000, ref lockTaken);
                        if (lockTaken)
                        {
                            Thread.Sleep(100);
                            sum += number;
                            Console.WriteLine($"number = {number}");
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            spinLock.Exit();
                            Console.WriteLine("release lock");
                        }
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