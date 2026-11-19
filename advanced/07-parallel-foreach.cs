// 主题：Parallel.ForEach —— 并行遍历集合；ParallelOptions；带索引和线程本地变量的重载

namespace HelloWorld
{
    internal class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Balance { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // ---------- 1. 最简单的用法：并行版 foreach（顺序不固定，同步执行完才返回） ----------
            string[] source = ["Hello", "Parallel", "ForEach"];
            Parallel.ForEach(source, item =>
            {
                Console.WriteLine($"{item} 长度为 {item.Length}");
            });

            Console.WriteLine("--------------------------");

            // 只在主线程创建、并行循环里只读，用普通 List 就行；
            // 多个线程同时往集合里写，才需要 ConcurrentBag / ConcurrentQueue / ConcurrentDictionary
            List<Person> persons =
            [
                new Person { Name = "刘备" },
                new Person { Name = "关羽" },
                new Person { Name = "张飞" },
            ];

            // ---------- 2. ParallelOptions：限制并发数、支持取消 ----------
            using var cts = new CancellationTokenSource();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 2,      // 最多同时用 2 个线程（默认不限制）
                CancellationToken = cts.Token,
            };

            long totalBalance = 0;

            // ---------- 3. 完整重载：<元素类型, 线程本地小计类型> + 索引 ----------
            ParallelLoopResult result = Parallel.ForEach<Person, int>(
                persons,
                options,
                () => 0,                                     // ① localInit：每个线程的小计从 0 开始
                (person, loop, index, balance) =>            // ② body：(元素, 循环状态, 索引, 当前小计)
                {
                    // Random.Shared 线程安全；Next 包含下限、不包含上限 → 10~99
                    // （.NET Framework 里要用 Guid 生成种子避免重复，现代 .NET 不需要）
                    int random = Random.Shared.Next(10, 100);
                    person.Balance = random;
                    Console.WriteLine($"{index}: {person.Name}: {person.Balance}  (线程 {Environment.CurrentManagedThreadId})");
                    return balance + random;                 // 返回新的小计
                },
                balance => Interlocked.Add(ref totalBalance, balance)   // ③ localFinally：汇总
            );

            Console.WriteLine($"IsCompleted = {result.IsCompleted}");
            Console.WriteLine($"totalBalance = {totalBalance}, 验证: {persons.Sum(p => p.Balance)}");
        }
    }
}
