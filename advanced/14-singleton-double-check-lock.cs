// 主题：线程安全的单例（一）：双重检查锁 + volatile

namespace HelloWorld
{
    internal sealed class Singleton
    {
        public string GUID { get; } = Guid.NewGuid().ToString();

        static readonly Lock locker = new();

        static volatile Singleton? instance;

        Singleton()
        {

        }

        public static Singleton GetSingleton()
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new Singleton();
                    }
                }
            }

            return instance;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                var task = Task.Run(() =>
                {
                    var guid = Singleton.GetSingleton().GUID;
                    var taskId = Task.CurrentId.ToString();
                    Console.WriteLine($"task: {taskId}, guid: {guid}");
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}