// 主题：ConcurrentDictionary —— 线程安全的字典（对应 Java 的 ConcurrentHashMap），用 TryAdd / TryGetValue / AddOrUpdate / GetOrAdd

using System.Collections.Concurrent;   // 并发集合不在隐式 using 里，要手动引入

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var dict = new ConcurrentDictionary<int, string>();

            var tasks = new List<Task>();

            for (int i = 0; i < 5; i++)
            {
                int number = i;
                var task = Task.Run(() =>
                {
                    string text = $"{number} * {number} = {number * number}";
                    dict.TryAdd(number, text);
                    if (dict.TryGetValue(number, out var value))   // 写 out string value 会报 CS8600：返回 false 时 value 是 null
                    {
                        Console.WriteLine($"key => {number}, value => {value}");
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            foreach (KeyValuePair<int, string> entry in dict)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value}");
            }
        }
    }
}