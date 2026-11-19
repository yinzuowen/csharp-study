// 主题：AssemblyLoadContext —— 代替 AppDomain 实现"加载程序集后再卸载"（插件场景）
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"默认上下文: {AssemblyLoadContext.Default.Name}");

            WeakReference weak = CreateAndUnload();

            // Unload() 只是发起卸载，真正释放要等 GC；还有对象引用着它就卸载不掉
            for (int i = 0; weak.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine($"卸载后还活着吗: {weak.IsAlive}");
        }

        // 必须放在单独的方法里：Debug 模式下局部变量会活到方法结束，GC 回收不了
        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference CreateAndUnload()
        {
            var context = new AssemblyLoadContext("myContext", isCollectible: true);   // isCollectible 才能卸载
            Console.WriteLine($"新建: {context.Name}, 可卸载: {context.IsCollectible}");
            // 实际使用：var asm = context.LoadFromAssemblyPath("/path/plugin.dll");
            context.Unload();
            return new WeakReference(context);
        }
    }
}
