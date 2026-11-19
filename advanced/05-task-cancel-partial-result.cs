// 主题：在任务内部检查取消，并返回取消前已算出的部分结果；以及两个 token 各自的作用

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            using var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // 第 1 个 token 传给 Calc：运行中检查，收到取消就退出循环（真正让计算停下来的是它）
            // 第 2 个 token 传给 Task.Run：任务还没开始就被取消时直接跳过；
            //                             任务抛 OperationCanceledException 时把状态标成 Canceled
            Task<int> task = Task.Run(() => Calc(9, token), token);

            cts.CancelAfter(3000);                      // 3 秒后取消（相当于 Wait(3000) 再 Cancel()）
            task.Wait();                                // 取消之后还要等任务真正退出，否则 Main 先结束

            // Calc 是安静退出的（没抛异常），所以状态是 RanToCompletion，而不是 Canceled
            Console.WriteLine($"Status = {task.Status}, 取消前算到的 sum = {task.Result}");

            Console.WriteLine("main thread end");
        }

        static int Calc(int max, CancellationToken token)
        {
            int sum = 0;
            int i = 0;
            while (!token.IsCancellationRequested && i <= max)
            {
                sum += i++;
                Console.WriteLine($"sum = {sum}");
                Thread.Sleep(1000);
            }
            // 想让状态变成 Canceled：把循环条件里的检查改成 token.ThrowIfCancellationRequested();
            return sum;
        }
    }
}
