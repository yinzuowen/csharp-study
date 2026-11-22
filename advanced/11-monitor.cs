// 主题：Monitor.Enter / Exit —— lock 语句展开后就是它，Exit 必须放在 finally 里

namespace HelloWorld
{
    internal class Person
    {
        public int Balance { get; set; }
    }

    internal class Program
    {
        static Person person = new Person { Balance = 100 };
        // Monitor 要用普通 object 做锁对象；传 System.Threading.Lock 会报 CS9216（Lock 只配合 lock 语句或 locker.Enter() 使用）
        static readonly object locker = new();

        static async Task Main(string[] args)
        {
            Task t1 = Task.Run(() => Withdraw(20));
            Task t2 = Task.Run(() => Withdraw(40));
            Task t3 = Task.Run(() => Withdraw(60));
            Task t4 = Task.Run(() => Withdraw(80));

            await Task.WhenAll(t1, t2, t3, t4);      // 不等的话 Main 立刻结束，任务来不及执行
            Console.WriteLine($"余额：{person.Balance}");
        }

        static void Withdraw(int money)
        {
            Monitor.Enter(locker);
            try
            {
                if (person.Balance >= money)
                {
                    Thread.Sleep(500);
                    Console.WriteLine($"成功取出 {money} 元");
                    person.Balance -= money;
                }
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }
    }
}