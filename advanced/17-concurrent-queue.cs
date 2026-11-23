// 主题：ConcurrentQueue —— 线程安全的队列，先进先出：Enqueue 入队，TryDequeue 出队（队列为空时返回 false，不阻塞）

using System.Collections.Concurrent;   // 并发集合不在隐式 using 里，要手动引入

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var queue = new ConcurrentQueue<int>();

            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                var number = i;

                var task = Task.Run(() =>
                {
                    queue.Enqueue(number);
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            Task consumer = Task.Run(() =>
            {
                while (queue.TryDequeue(out int number))
                {
                    Console.WriteLine($"number: {number}");
                }
            });

            consumer.Wait();
        }
    }
}