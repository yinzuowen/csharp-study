// 主题：System.Timers.Timer —— Elapsed 事件、Start / Stop、AutoReset
//
// 和 System.Threading.Timer 的区别：
//   回调方式：Elapsed 事件（可以订阅多个）     vs  构造函数里传 TimerCallback
//   启停：Start() / Stop() / Enabled           vs  Change(dueTime, period)
//   只触发一次：AutoReset = false              vs  period = Timeout.Infinite
//   回调里的异常：被吞掉，看不到（坑）          vs  让进程崩溃
using System.Timers;

namespace HelloWorld
{
    internal class Person
    {
        public int Balance { get; set; }
    }

    internal class Program
    {
        static Person person = new Person { Balance = 100 };

        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            // 写全称：System.Threading 里也有一个 Timer
            using System.Timers.Timer timer = new System.Timers.Timer(1000);
            timer.AutoReset = true;                  // true：每隔 1 秒触发一次；false：只触发一次
            timer.Elapsed += TimerElapsed;
            timer.Start();

            // 原来写的是 Task.Delay(5500).ContinueWith(t => timer.Stop()) 且没有等待：
            // Main 马上结束，using 把 timer 释放、进程退出，Elapsed 一次都来不及触发
            await Task.Delay(5500);
            timer.Stop();

            Console.WriteLine("main thread end");
        }

        static void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"{e.SignalTime:HH:mm:ss} balance = {person.Balance}");
            person.Balance -= 20;
        }
    }
}
