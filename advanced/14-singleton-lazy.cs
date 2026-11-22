// 主题：线程安全的单例（二）：Lazy<T> —— 第一次访问 .Value 才创建，默认就是线程安全的

namespace HelloWorld
{
    internal sealed class Singleton
    {
        public string GUID { get; } = Guid.NewGuid().ToString();

        // 注意不能写成 public static Singleton Instance = new Lazy<Singleton>().Value;
        // 那样在静态字段初始化时就立刻取了 .Value，对象马上被创建，就不是"延迟"了
        static readonly Lazy<Singleton> lazy = new Lazy<Singleton>(() => new Singleton());

        public static Singleton Instance => lazy.Value;

        // 传入工厂方法 () => new Singleton() 时，构造函数可以是 private
        // 只有用无参的 new Lazy<Singleton>() 时，才要求 public 无参构造函数（内部用反射创建）
        Singleton()
        {
            Console.WriteLine("Singleton 构造函数执行（只会执行一次）");
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
                    var guid = Singleton.Instance.GUID;
                    var taskId = Task.CurrentId.ToString();
                    Console.WriteLine($"task: {taskId}, guid: {guid}");
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}