// 主题：PeriodicTimer（.NET 6+）—— 专为 async 设计的定时器，在自己的循环里 await 下一次触发
//
// System.Threading.Timer：定时器"推着你走"，到时间就在线程池上调回调，回调可能重叠执行
// PeriodicTimer：你主动"拉"，代码从上往下执行，不会重叠，也不需要额外的结束信号

namespace HelloWorld
{
    internal class Person
    {
        public int Balance { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            Person person = new Person { Balance = 100 };

            await Task.Delay(1000);                                       // 第一次延迟 1 秒
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            do
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} balance = {person.Balance}");
                person.Balance -= 20;
            }
            while (person.Balance > 0 && await timer.WaitForNextTickAsync());   // 每 1 秒一次

            // 支持取消：await timer.WaitForNextTickAsync(token)；timer.Dispose() 后返回 false，循环结束

            Console.WriteLine("main thread end");
        }
    }
}
