// 主题：把 Parallel 放进 Task.Run —— Parallel 负责"内部并行"，Task.Run 负责"对外不阻塞"

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            // Parallel.Invoke 本身会阻塞调用方；包进 Task.Run 后，主线程可以立即往下执行
            Task task = Task.Run(() =>
            {
                Parallel.Invoke(() => Square(2), () => Cube(3));
            });

            Console.WriteLine("main thread is doing other work...");   // 会比 Square/Cube 的结果先打印

            await task;   // 不等的话 Main 先结束，后台线程被终止，什么都看不到

            // 注意：如果 Task.Run 之后马上 task.Wait()，就和直接调用 Parallel.Invoke 没区别，还多占一个线程
            // 典型用途：UI 程序里 await Task.Run(() => Parallel.For(...))，界面不会卡住

            Console.WriteLine("main thread end");
        }

        static void Square(int a)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{a} * {a} = {a * a}");
        }

        static void Cube(int a)
        {
            Thread.Sleep(500);
            Console.WriteLine($"{a} * {a} * {a} = {a * a * a}");
        }
    }
}
