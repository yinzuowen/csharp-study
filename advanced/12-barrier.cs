// 主题：Barrier —— 所有参与者都到达屏障后才一起继续，可以在放行前执行一个回调（类似 Java 的 CyclicBarrier）

namespace HelloWorld
{
    internal class Program
    {
        static readonly string[] names = ["Alice", "Bob", "Chris"];
        static readonly Barrier barrier = new Barrier(names.Length, Close);

        static void Main(string[] args)
        {
            List<Task> tasks = new List<Task>(names.Length);

            int i = 1;

            foreach (string name in names)
            {
                int seconds = i++;                   // 先在循环里取值；写成 Exit(name, i++) 的话，i++ 要等任务执行时才算，几个任务会抢同一个 i
                Task task = Task.Run(() => Exit(name, seconds));
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }

        static void Exit(string name, int seconds)
        {
            Thread.Sleep(seconds * 1000);
            Console.WriteLine($"{name} 耗时 {seconds} 秒下机");
            barrier.SignalAndWait();
            Console.WriteLine($"{name} 打车回家");
        }

        static void Close(Barrier barrier)
        {
            Console.WriteLine($"{barrier.ParticipantCount} 名乘客全部下机，请关闭舱门");
        }
    }
}