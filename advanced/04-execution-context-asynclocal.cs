// 主题：ExecutionContext 流动 + AsyncLocal<T>
//
// CallContext.LogicalSetData / LogicalGetData 是 .NET Framework 的 API（属于 Remoting），
// 现代 .NET 里没有这个类，用 AsyncLocal<T> 代替。

namespace HelloWorld
{
    internal static class Context
    {
        // ThreadLocal：属于某个线程，换线程就读不到
        // AsyncLocal：属于某条执行流，会跟着 Task.Run / await / 线程池 流过去
        public static readonly AsyncLocal<Person?> CurrentPerson = new();
    }

    internal class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var person = new Person { Name = "Owen", Age = 36 };
            Context.CurrentPerson.Value = person;        // 相当于 CallContext.LogicalSetData("person", person)

            using var done = new CountdownEvent(3);

            // 把任务交给别的线程时，.NET 会把当前的执行上下文复制过去 → 能读到 Owen
            ThreadPool.QueueUserWorkItem(_ => { Show("默认"); done.Signal(); });

            // 暂停流动 → 读不到
            ExecutionContext.SuppressFlow();
            ThreadPool.QueueUserWorkItem(_ => { Show("SuppressFlow"); done.Signal(); });
            ExecutionContext.RestoreFlow();

            // 恢复流动 → 又能读到
            ThreadPool.QueueUserWorkItem(_ => { Show("RestoreFlow"); done.Signal(); });

            done.Wait();                                 // 用 CountdownEvent 等，比 Thread.Sleep 可靠
        }

        static void Show(string label)
        {
            Person? p = Context.CurrentPerson.Value;  // 相当于 CallContext.LogicalGetData("person")
            Console.WriteLine($"[{label}] 线程 {Environment.CurrentManagedThreadId}: " +
                              (p is null ? "person is empty" : $"person: {p.Name}, {p.Age}"));
        }
    }
}
