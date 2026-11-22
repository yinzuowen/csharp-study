// 主题：[MethodImpl(MethodImplOptions.Synchronized)] —— 整个方法加锁（静态方法锁的是 typeof(Program)），不推荐，了解即可

using System.Runtime.CompilerServices;

namespace HelloWorld
{
    internal class Person
    {
        public int Balance { get; set; }
    }

    internal class Program
    {
        static Person person = new Person { Balance = 100 };

        static async Task Main(string[] args)
        {
            Task t1 = Task.Run(() => Withdraw(20));
            Task t2 = Task.Run(() => Withdraw(40));
            Task t3 = Task.Run(() => Withdraw(60));
            Task t4 = Task.Run(() => Withdraw(80));

            await Task.WhenAll(t1, t2, t3, t4);      // 不等的话 Main 立刻结束，任务来不及执行
            Console.WriteLine($"余额：{person.Balance}");
        }

        [MethodImplAttribute(MethodImplOptions.Synchronized)]
        static void Withdraw(int money)
        {
            if (person.Balance >= money)
            {
                Thread.Sleep(500);
                Console.WriteLine($"成功取出 {money} 元");
                person.Balance -= money;
            }
        }
    }
}