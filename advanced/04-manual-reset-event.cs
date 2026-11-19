// 主题：ManualResetEvent + WaitHandle.WaitAll —— 每个任务一盏"信号灯"，全亮了主线程再继续
//
// new ManualResetEvent(false)：灭灯；Set()：点亮；Reset()：熄灭；WaitOne()：等这一盏亮
// Manual：亮了会一直亮；AutoResetEvent 放行一个等待者后会自动熄灭

namespace HelloWorld
{
    internal class Model
    {
        public ManualResetEvent Wait { get; }
        public int Number { get; private set; }
        public int Result { get; private set; }
        public int Milliseconds { get; private set; }

        public Model(ManualResetEvent wait) => Wait = wait;

        public void Calc(object? param)
        {
            if (param is int number)
            {
                Number = number;
                Result = number * number;
            }

            // .NET Framework 里短时间 new Random() 种子会相同，现代 .NET 没这个问题；推荐用 Random.Shared
            Milliseconds = Random.Shared.Next(0, 500);
            Thread.Sleep(Milliseconds);

            Wait.Set();                           // 点亮：我完成了
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<Model> models = [];
            List<ManualResetEvent> waits = [];

            for (int i = 0; i < 10; i++)
            {
                var model = new Model(new ManualResetEvent(false));
                models.Add(model);
                waits.Add(model.Wait);
                ThreadPool.QueueUserWorkItem(model.Calc, i);
            }

            WaitHandle.WaitAll(waits.ToArray());          // 等所有灯都亮（最多 64 个）

            // 完成顺序是随机的，但输出按 models 的创建顺序，所以结果总是有序的
            foreach (var item in models)
            {
                Console.WriteLine($"{item.Number} * {item.Number} = {item.Result}, sleep {item.Milliseconds}ms");
                item.Wait.Dispose();                      // 包装了系统句柄，用完释放
            }

            // 现代写法：Task.WhenAll，不需要信号灯，也没有 64 个的限制
            var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(0, 500));
                return i * i;
            }));
            int[] results = await Task.WhenAll(tasks);
            Console.WriteLine($"Task.WhenAll: {string.Join(", ", results)}");
        }
    }
}
