// 主题：事件（event）—— 受保护的委托：外部只能 += / -=，只有类自己能触发
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- 1. 事件可以用任意委托类型：自定义委托、Action、Func、Predicate ----------
var leader = new Leader();
leader.OnOrder += order => Console.WriteLine($"OnOrder: {order}");
leader.OnStart += order => Console.WriteLine($"OnStart: {order}");
leader.OnAttack += order => { Console.WriteLine($"OnAttack: {order}"); return 0; };
leader.OnCheck += order => { Console.WriteLine($"OnCheck: {order}"); return false; };
// leader.OnOrder("xxx");              // ❌ 外部不能触发事件
// leader.OnOrder = null;              // ❌ 外部不能直接赋值，只能 += / -=
leader.Start();

Console.WriteLine("--------------------------");

// ---------- 2. 标准写法：EventHandler(object? sender, EventArgs e) ----------
var button = new Button();
button.Click += Button_Click;                       // 订阅
button.Click -= Button_Click;                       // 取消订阅
button.Click += new EventHandler(Button_Click);     // 老写法，和 += Button_Click 一样
button.Click += (sender, e) => Console.WriteLine($"lambda: {sender}");
button.Click += delegate (object? sender, EventArgs e)   // 匿名方法写法
{
    Console.WriteLine($"anonymous: {sender}");
};
button.Start();

Console.WriteLine("--------------------------");

// ---------- 3. 自定义事件参数：EventHandler<TEventArgs> ----------
var clock = new Clock();
clock.OnRing += (sender, e) => Console.WriteLine($"ring at {e.Time:HH:mm:ss}, sender: {sender}");
clock.Start();

Console.WriteLine("--------------------------");

// ---------- 4. 静态事件：属于类本身，不需要实例 ----------
StaticClock.OnRing += time => Console.WriteLine($"static ring at {time:HH:mm:ss}");
StaticClock.Start();

static void Button_Click(object? sender, EventArgs e)
{
    Console.WriteLine($"Button_Click: {sender}");
}

class Leader
{
    public delegate void OrderHandler(string order);

    // 事件是成员，不是类型：event + 委托类型 + 名字
    // 加 ? 是因为没人订阅时它就是 null（否则有 CS8618 警告）
    public event OrderHandler? OnOrder;
    public event Action<string>? OnStart;
    public event Func<string, int>? OnAttack;
    public event Predicate<string>? OnCheck;

    public void Start()
    {
        OnOrder?.Invoke("接令");      // ?. ：没人订阅时不调用，避免 NullReferenceException
        OnStart?.Invoke("出发");
        OnAttack?.Invoke("攻击");
        OnCheck?.Invoke("清点");
    }
}

class Button
{
    public event EventHandler? Click;

    public void Start()
    {
        Click?.Invoke(this, EventArgs.Empty);   // sender 传 this，没有参数时用 EventArgs.Empty
    }

    public override string ToString() => "Button#1";
}

// 自定义事件参数：按惯例继承 EventArgs，类名以 EventArgs 结尾
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
    private readonly Lock _lock = new();      // C# 13 / .NET 9 的专用锁类型
    public event EventHandler<ClockEventArgs>? OnRing;

    public void Start()
    {
        lock (_lock)
        {
            OnRing?.Invoke(this, new ClockEventArgs(DateTime.Now));
        }
    }
}

class StaticClock
{
    public static event Action<DateTime>? OnRing;

    public static void Start() => OnRing?.Invoke(DateTime.Now);
}

// 注：原 hello29 用的 WeakEventManager 属于 WPF（System.Windows），只能在 Windows 上用，这里没有收录
