// 主题：常用内置特性 —— [Obsolete]、[Conditional]
using System.Diagnostics;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            #pragma warning disable CS0618            // 演示用：关闭"调用了过时方法"的警告
            OldShow("Hello World");
            #pragma warning restore CS0618
            NewShow("Hello World");

            // [Conditional("DEBUG")]：非 DEBUG 编译时，所有调用它的地方都会被编译器删掉
            Log("这一行只在 Debug 编译时输出");

            #if DEBUG
            Console.WriteLine("#if DEBUG：效果类似，但删的是这段代码本身");
            #endif
        }

        // [Obsolete(消息, 是否报错)]：false = 警告 CS0618，true = 编译错误
        [Obsolete("请改用 NewShow", false)]
        static void OldShow(string message) => Console.WriteLine($"old: {message}");

        static void NewShow(string message) => Console.WriteLine($"new: {message}");

        // 只能用在返回 void 的方法上；加在 Main 上没有意义（Main 不是被代码调用的）
        [Conditional("DEBUG")]
        static void Log(string message) => Console.WriteLine($"[DEBUG] {message}");
    }
}
