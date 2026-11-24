#:property PublishAot=false

using System.Windows;

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 事件
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
        private readonly Lock locker = new Lock();
        public event Action<object, ClockEventArgs>? OnRing;

        public void Start()
        {
            lock (locker)
            {
                OnRing?.Invoke(this, new ClockEventArgs(DateTime.Now));
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Clock clock = new Clock();

            WeakEventManager<Clock, ClockEventArgs>.AddHandler(clock, "OnRing", (sender, args) =>
            {
                Console.WriteLine($"sender: {sender}, args: {args}");
            });

            clock.Start();
        }
    }
}