// 主题：new Task + Start() —— "冷启动"：创建后必须调用 Start 才会执行

// 写法 1：无参 Action，参数通过闭包写在 lambda 里

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Task task = new Task(() => DoJob(123));       // 等价于：Action job = () => DoJob(123); new Task(job);
            task.Start();

            // 写法 2：Action<object?> + state，参数作为 Task 构造函数的第 2 个参数
            Task task2 = new Task(state => DoJob(state), 456);
            task2.Start();

            // 有返回值：Task<TResult> + Func<object?, TResult>
            var task3 = new Task<int>(state => state is int n ? n * n : 0, 9);
            task3.Start();

            Console.WriteLine("main thread waiting...");
            Task.WaitAll(task, task2);                    // Task 没有 Result，不等的话 Main 先结束，任务来不及执行
            Console.WriteLine($"task3.Result = {task3.Result}");   // .Result 本身就会阻塞等待
            Console.WriteLine("main thread end");
        }

        // Task 的 state 是 object?，参数也要写 object?
        static void DoJob(object? obj)
        {
            Console.WriteLine(obj);
            Thread.Sleep(1000);
            Console.WriteLine($"do job {obj} completed");
        }
    }
}
