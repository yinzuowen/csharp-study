#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 事件
    // public delegate void OrderHandler(string order);
    // public event OrderHandler? OnOrder;
    // public event Action<string>? OnAction;
    // public event Func<string, int>? OnFunc;
    // public event Predicate<string>? OnPredicate;

    internal class Leader
    {
        public delegate void OrderHandler(string order);
        public event OrderHandler? OnOrder;
        public event Action<string>? OnStart;
        public event Func<string, int>? OnAttack;
        public event Predicate<string>? OnCheck;

        public void Start()
        {
            OnOrder?.Invoke("接令");
            OnStart?.Invoke("出发");
            OnAttack?.Invoke("攻击");
            OnCheck?.Invoke("清点");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Leader leader = new Leader();

            leader.OnOrder += Leader_OnOrder;
            leader.OnStart += Leader_OnStart;
            leader.OnAttack += Leader_OnAttack;
            leader.OnCheck += Leader_OnCheck;

            Thread.Sleep(3000);
            leader.Start();
        }

        private static void Leader_OnOrder(string order)
        {
            Console.WriteLine(order);
        }

        private static void Leader_OnStart(string order)
        {
            Console.WriteLine(order);
        }

        private static int Leader_OnAttack(string order)
        {
            Console.WriteLine(order);
            return 0;
        }

        private static bool Leader_OnCheck(string order)
        {
            Console.WriteLine(order);
            return false;
        }
    }
}