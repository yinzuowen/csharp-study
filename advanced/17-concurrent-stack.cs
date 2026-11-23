// 主题：ConcurrentStack —— 线程安全的栈，后进先出；普通 Stack 在多线程下会重复弹出、丢数据，甚至抛异常

using System.Collections.Concurrent;   // 并发集合不在隐式 using 里，要手动引入

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            int[] array = [1, 2, 3, 4, 5];

            var stack = new ConcurrentStack<int>(array);

            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                var task = Task.Run(() =>
                {
                    if (stack.TryPop(out int item))
                    {
                        Console.WriteLine($"item = {item}");
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}