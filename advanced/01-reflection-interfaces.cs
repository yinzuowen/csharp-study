#:property PublishAot=false
// 主题：反射 —— 获取类型实现的接口、判断是否实现某接口

namespace HelloWorld
{
    internal interface IRun
    {
        event EventHandler<string>? OnMeow;
    }

    internal interface ISwim
    {
    }

    internal class Cat : IRun, ISwim
    {
        public event EventHandler<string>? OnMeow;
        public void Meow() => OnMeow?.Invoke(this, "meow");
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Type type = typeof(Cat);

            foreach (Type item in type.GetInterfaces())
            {
                Console.WriteLine($"{item.MemberType}, {item.FullName}");
            }

            Console.WriteLine($"Cat 实现了 IRun: {typeof(IRun).IsAssignableFrom(type)}");
            Console.WriteLine($"Cat 实现了 IDisposable: {typeof(IDisposable).IsAssignableFrom(type)}");
        }
    }
}
