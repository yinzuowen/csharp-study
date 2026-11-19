#:property PublishAot=false
// 主题：反射 —— 通过 EventInfo 订阅 / 取消订阅事件
using System.Reflection;

namespace HelloWorld
{
    internal class Cat
    {
        public event EventHandler<string>? OnMeow;

        // 事件只能在类内部触发
        public void Meow(string target) => OnMeow?.Invoke(this, target);
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Type type = typeof(Cat);
            if (Activator.CreateInstance(type) is not Cat cat) return;
            EventInfo eventInfo = type.GetEvent("OnMeow") ?? throw new InvalidOperationException();

            // 委托类型必须和事件类型一致：EventHandler<string> ≠ EventHandler
            EventHandler<string> handler = OnMeowCallback;

            eventInfo.AddEventHandler(cat, handler);       // 相当于 cat.OnMeow += handler
            cat.Meow("mouse");                             // 触发事件 → 调用 OnMeowCallback

            eventInfo.RemoveEventHandler(cat, handler);    // 相当于 cat.OnMeow -= handler
            cat.Meow("fish");                              // 没人订阅，不输出

            // 注意：handler.Invoke(cat, "mouse") 是直接调用委托，不经过事件，证明不了订阅成功

            // 从外部强行触发（只用于练习）：简写事件背后有一个同名的私有委托字段
            eventInfo.AddEventHandler(cat, handler);
            FieldInfo? field = type.GetField("OnMeow", BindingFlags.NonPublic | BindingFlags.Instance);
            (field?.GetValue(cat) as EventHandler<string>)?.Invoke(cat, "bird (via backing field)");
        }

        static void OnMeowCallback(object? sender, string target)
        {
            Console.WriteLine($"{sender}: meow at {target}");
        }
    }
}
