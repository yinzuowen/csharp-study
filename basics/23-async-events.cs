// 主题：异步事件 —— 委托返回 Task，触发方可以 await 订阅者执行完
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

var clock = new Clock();
clock.OnRing += Clock_OnRing;
clock.OnRing += async (sender, e) =>
{
    await Task.Delay(500);
    Console.WriteLine("second subscriber done");
};

// 必须一路 await 到顶层：如果这里不 await，程序会直接结束，订阅者的后半段来不及打印
await clock.Start();
Console.WriteLine("main end");

static async Task Clock_OnRing(object sender, ClockEventArgs e)
{
    Console.WriteLine("first subscriber start");
    await Task.Delay(1000);
    Console.WriteLine($"current time: {e.Time:HH:mm:ss}");
    Console.WriteLine("first subscriber end");
}

class ClockEventArgs : EventArgs
{
    public DateTime Time { get; }

    public ClockEventArgs(DateTime time)
    {
        Time = time;
    }
}

class Clock
{
    public event Func<object, ClockEventArgs, Task>? OnRing;

    public async Task Start()
    {
        if (OnRing is null) return;   // 没人订阅：await null 会抛异常，所以先判断

        // 只有一个订阅者时可以直接 await OnRing.Invoke(...)；
        // 多个订阅者时 Invoke 只返回最后一个的 Task，所以要逐个取出来一起等
        var args = new ClockEventArgs(DateTime.Now);
        var tasks = OnRing.GetInvocationList()
            .Cast<Func<object, ClockEventArgs, Task>>()
            .Select(handler => handler(this, args));
        await Task.WhenAll(tasks);
    }
}
