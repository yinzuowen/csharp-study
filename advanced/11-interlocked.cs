// 主题：Interlocked —— count++ 不是原子操作，多线程下会丢失更新；Interlocked.Increment 是原子的

namespace HelloWorld
{
    internal class Program
    {
        static int count, length;

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 9999; i++)
            {
                var task = Task.Run(() =>
                {
                    count++;
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"count = {count}");

            tasks.Clear();

            for (int i = 0; i < 9999; i++)
            {
                var task = Task.Run(() =>
                {
                    Interlocked.Increment(ref length);
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine($"length = {length}");

            Console.WriteLine("main thread end");
        }
    }
}