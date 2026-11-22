// 主题：读写锁 —— 读和读可以并发，写和任何操作互斥；新代码用 ReaderWriterLockSlim

namespace HelloWorld
{
    internal class Person
    {
        public int Balance { get; set; }
    }
    internal class Program
    {
        static Person person = new Person();
        // ReaderWriterLock 是老 API，新代码用 ReaderWriterLockSlim（EnterReadLock / ExitReadLock），更快也更不容易出错
        static ReaderWriterLock readerWriterLock = new ReaderWriterLock();

        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            List<Task> tasks = new List<Task>();

            var task1 = Task.Run(Write);
            var task2 = Task.Run(Read);
            var task3 = Task.Run(Read);
            var task4 = Task.Run(Read);

            tasks.Add(task1);
            tasks.Add(task2);
            tasks.Add(task3);
            tasks.Add(task4);

            Task.WaitAll(tasks.ToArray());

            Console.WriteLine("main thread end");
        }

        static void Write()
        {
            var id = $"[{Thread.CurrentThread.ManagedThreadId}]";
            readerWriterLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                Thread.Sleep(500);
                person.Balance += 10;
                Console.WriteLine($"{id} Balance += 10");
            }
            finally
            {
                readerWriterLock.ReleaseWriterLock();
            }
        }

        static void Read()
        {
            var id = $"[{Thread.CurrentThread.ManagedThreadId}]";
            readerWriterLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                Thread.Sleep(500);
                Console.WriteLine($"{id} Balance = {person.Balance}");
            }
            finally
            {
                readerWriterLock.ReleaseReaderLock();
            }
        }
    }
}