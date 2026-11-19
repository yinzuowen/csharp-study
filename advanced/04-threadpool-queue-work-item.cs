// 主题：ThreadPool.QueueUserWorkItem —— 把任务放进线程池队列，用 CountdownEvent 等全部完成

// QueueUserWorkItem 只是排队，立即返回；线程池线程是后台线程，Main 不等就会直接退出、没有输出

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using var done = new CountdownEvent(10);     // 计数器，初始值 10

            for (int i = 0; i < 10; i++)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    Calc(state);
                    done.Signal();                       // 完成一个，计数减 1
                }, i);
            }

            done.Wait();                                 // 阻塞，直到计数减到 0
            Console.WriteLine("all done（输出顺序每次可能不同）");
        }

        // WaitCallback 的参数是 object?
        static void Calc(object? param)
        {
            if (param is int number)
            {
                Console.WriteLine($"{number} * {number} = {number * number}");
            }
        }
    }
}
