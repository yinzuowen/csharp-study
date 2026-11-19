// 主题：Task.Run —— 最常用的写法；没有 state 参数，只能用闭包传参
//
// 能接收的委托：
//   Action                → Task          () => { ... }
//   Func<TResult>         → Task<TResult> () => 值
//   Func<Task>            → Task          async () => { await ...; }
//   Func<Task<TResult>>   → Task<TResult> async () => { await ...; return 值; }   ← 自动解包，不是 Task<Task<T>>

// Action

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Task t1 = Task.Run(() => Console.WriteLine("Action"));

            // Func<TResult>
            Task<int> t2 = Task.Run(() => DoJob(123));
            Task<int> t3 = Task.Run(() => DoJob(456));
            // Task.Run(DoJob, 456);                 // ❌ Task.Run 没有 state 重载

            // Func<Task>：async lambda 不 return 值，编译器自动生成并返回 Task
            Task t4 = Task.Run(async () =>
            {
                await Task.Delay(500);
                Console.WriteLine("Func<Task> done");
            });

            // Func<Task<TResult>>：async lambda 里 return int，编译器把它包装成 Task<int>
            Task<int> t5 = Task.Run(async () =>
            {
                await Task.Delay(500);
                return 999;
            });

            await Task.WhenAll(t1, t4);
            // await Task.WhenAll 直接得到结果数组，顺序和传入的任务一致
            int[] results = await Task.WhenAll(t2, t3, t5);
            Console.WriteLine($"results = {string.Join(", ", results)}");

            // 不用 async 的 Func<Task>：直接返回一个 Task 对象
            Func<Task> delay = () => Task.Delay(100);
            await delay();
        }

        static int DoJob(int n)
        {
            Thread.Sleep(1000);
            return n + 100;
        }
    }
}
