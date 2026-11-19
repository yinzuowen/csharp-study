// 主题：批量创建任务 + AsyncState 取回参数；以及"在线程池里阻塞"有多慢
using System.Diagnostics;

namespace HelloWorld
{
    internal class Entity
    {
        public int Number { get; }
        public int Result { get; set; }

        public Entity(int number)
        {
            Number = number;
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            int max = 100;
            List<Entity> entities = Enumerable.Range(1, max).Select(i => new Entity(i)).ToList();

            // ---------- 1. Thread.Sleep：阻塞线程池线程 ----------
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<Task> tasks = new List<Task>();

            foreach (Entity item in entities)
            {
                Task task = Task.Factory.StartNew(param =>
                {
                    if (param is Entity entity)
                    {
                        Thread.Sleep(100);
                        entity.Result = entity.Number;
                    }
                }, item);
                tasks.Add(task);                        // 别忘了加进列表，否则 WaitAll 什么都不等，结果是 0
            }

            Task.WaitAll(tasks.ToArray());

            int sum = 0;
            foreach (var item in tasks)
            {
                // AsyncState 就是创建任务时传入的 state（这里是 Entity），类型是 object?
                if (item.AsyncState is Entity entity)
                {
                    sum += entity.Result;
                }
            }

            // 线程池一开始只有 CPU 核数个线程，之后约每 0.5 秒才加一个，所以大约是"几个一组"地执行
            Console.WriteLine($"Thread.Sleep 版：1 到 {max} 的和 = {sum}，耗时 {stopwatch.ElapsedMilliseconds}ms");

            // ---------- 2. await Task.Delay：等待期间不占线程 ----------
            stopwatch.Restart();
            int[] results = await Task.WhenAll(entities.Select(async entity =>
            {
                await Task.Delay(100);
                return entity.Number;
            }));
            Console.WriteLine($"Task.Delay 版：1 到 {max} 的和 = {results.Sum()}，耗时 {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
