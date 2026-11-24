#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 异步事件
    internal class ClockEventArgs : EventArgs
    {
        public DateTime DateTime { get; }
        public ClockEventArgs(DateTime dateTime)
        {
            DateTime = dateTime;
        }
    }

    internal class Clock
    {
        public event Func<object, ClockEventArgs, Task>? OnRing;
        public async Task Start()
        {
            if (OnRing is null) return;   // 没人订阅：await null 会抛异常，所以先判断
            await OnRing.Invoke(this, new ClockEventArgs(DateTime.Now));
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Clock clock = new Clock();
            clock.OnRing += Clock_OnRing;
            await clock.Start();
        }

        private static async Task Clock_OnRing(object sender, ClockEventArgs args)
        {
            Console.WriteLine("async start");
            await Task.Delay(3000);
            Console.WriteLine($"current time: {args.DateTime}");
            Console.WriteLine("async end");
        }
    }
}