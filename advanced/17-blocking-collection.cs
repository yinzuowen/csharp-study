// 主题：BlockingCollection —— 生产者-消费者：设置容量后，队列满了 Add 阻塞，队列空了 Take 阻塞（对应 Java 的 BlockingQueue）

using System.Collections.Concurrent;   // 并发集合不在隐式 using 里，要手动引入

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var collection = new BlockingCollection<int>(5);

            var producer = Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    collection.Add(i);
                    Console.WriteLine($"已生产: {i}");
                    Thread.Sleep(1000);
                }

                collection.CompleteAdding();
            });

            var consumer = Task.Run(() =>
            {
                // 更简洁的写法：foreach (int item in collection.GetConsumingEnumerable())，不需要 try/catch
                while (!collection.IsCompleted)
                {
                    try
                    {
                        int item = collection.Take();
                        Console.WriteLine($"已消费: {item}");
                        Thread.Sleep(500);
                    }
                    catch (InvalidOperationException)
                    {
                        // 检查 IsCompleted 之后、调用 Take 之前，生产者可能刚好调用了 CompleteAdding，
                        // 这时 Take 会抛 InvalidOperationException，表示已经没有数据了，忽略即可
                    }
                }
            });

            Task.WaitAll(producer, consumer);        // 等多个任务用静态的 Task.WaitAll；Task.Wait 是实例方法：task.Wait()
        }
    }
}