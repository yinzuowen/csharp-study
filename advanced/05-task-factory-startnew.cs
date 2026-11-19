// 主题：Task.Factory.StartNew —— 创建并立即启动，支持 state 参数
//
// 重载对应关系：
//   StartNew(Action)                              → Task
//   StartNew(Action<object?>, object? state)      → Task
//   StartNew(Func<TResult>)                       → Task<TResult>
//   StartNew(Func<object?, TResult>, object? state) → Task<TResult>
// 注意：state 重载只接受 Action<object?> / Func<object?, TResult>，不接受 Action<int>

// Action 没有返回值，DoJob 的返回值会被丢掉；用 Func 才能拿到 +100 之后的结果

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Func<object?, int> job = DoJob;              // 等价于 (x) => DoJob(x)

            Task<int> task = Task.Factory.StartNew(() => job(123));   // Func<int>：参数用闭包传
            Task<int> task2 = Task.Factory.StartNew(job, 456);        // Func<object?, int> + state
            Task<int> sum = Task.Factory.StartNew(Sum, 10);           // 直接传方法组

            Console.WriteLine("main thread waiting...");
            // .Result 会阻塞等到任务完成，所以这里不需要 Task.WaitAll
            Console.WriteLine($"task.Result = {task.Result}, task2.Result = {task2.Result}");
            Console.WriteLine($"0 + 1 + ... + 10 = {sum.Result}");
            Console.WriteLine("main thread end");
        }

        static int DoJob(object? obj)
        {
            Console.WriteLine(obj);
            Thread.Sleep(1000);
            return obj is int number ? number + 100 : 0;   // obj 可能是 null 或不是 int，先判断再用
        }

        static int Sum(object? obj)
        {
            if (obj is not int max) return 0;
            int total = 0;
            for (int i = 0; i <= max; i++)
            {
                total += i;
            }
            return total;
        }

        // StartNew 的两个坑（所以日常用 Task.Run）：
        // 1. 传 async lambda 会得到 Task<Task>，要 .Unwrap()
        // 2. 默认用"当前"TaskScheduler，在 UI 程序里可能跑到 UI 线程上
        // 需要 TaskCreationOptions.LongRunning 等特殊选项时才用 StartNew
    }
}
