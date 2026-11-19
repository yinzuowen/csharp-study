// 主题：async 方法的异常 —— 用 try/catch 包住 await 即可；注意 Main 要等它，以及不要写 async void

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 直接 await 就行，不需要 Task.Run(async () => await StartAsync()) 再包一层
            // 如果不 await（或者 Main 不等），程序会在异常抛出之前就结束，看起来像是"没捕获到"
            try
            {
                await StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            // async void：没有 Task 可以保存异常，异常会直接抛到线程池上 → 进程崩溃
            // 除了事件处理程序（button.Click += async (s, e) => ...），一律写 async Task
            // BadAsyncVoid();   // ← 取消注释试试：catch 不住，进程崩溃
            // await Task.Delay(1500);
        }

        static async Task StartAsync()
        {
            await Task.Delay(1000);
            throw new Exception("异步方法异常");
        }

        static async void BadAsyncVoid()
        {
            await Task.Delay(1000);
            throw new Exception("async void 异常");
        }
    }
}
