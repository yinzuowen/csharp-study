// 主题：Thread 里的异常 —— 没有捕获就会让整个进程崩溃，所以要在线程方法内部 try/catch

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Thread thread = new Thread(Method);
            thread.Start();
            thread.Join();

            // Thread 拿不到返回值，异常也传不回调用方：
            //   try { thread.Start(); } catch { }   ← 捕获不到，异常发生在另一个线程上
            // 线程里未捕获的异常会触发 AppDomain.UnhandledException，然后进程崩溃（见 09-global-exception-handlers.cs）
            Console.WriteLine("main thread end");
        }

        static void Method()
        {
            try
            {
                throw new Exception("子线程异常");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"线程内部捕获: {ex.Message}");
            }
        }
    }
}
