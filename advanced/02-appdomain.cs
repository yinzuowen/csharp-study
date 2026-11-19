// 主题：AppDomain —— 现代 .NET 里每个进程只有一个 AppDomain
//
// AppDomain.CreateDomain / Unload 是 .NET Framework 的功能，
// 在 .NET Core / .NET 5+ 里会抛 PlatformNotSupportedException（警告 SYSLIB0024）。
// 需要"加载后再卸载"时用 AssemblyLoadContext（见 02-assembly-load-context.cs），需要真正隔离时用独立进程。

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AppDomain current = AppDomain.CurrentDomain;
            Console.WriteLine($"FriendlyName:  {current.FriendlyName}");
            Console.WriteLine($"BaseDirectory: {current.BaseDirectory}");

            Console.WriteLine("已加载的程序集：");
            foreach (var assembly in current.GetAssemblies())
            {
                Console.WriteLine($"  {assembly.GetName().Name}");
            }

            // 现在还常用的：全局未处理异常事件
            current.UnhandledException += (sender, e) => Console.WriteLine($"未处理异常: {e.ExceptionObject}");
        }
    }
}
