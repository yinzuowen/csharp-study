// 主题：volatile —— 保证可见性：去掉 volatile，Release 模式下子线程可能永远看不到 Loop = false（Debug 下看不出区别）

namespace HelloWorld
{
    internal class Person
    {
        public int Balance { get; set; }
        // 每次读都从内存里取最新值，编译器和 JIT 不能把这次读缓存起来，也不能把它挪到循环外面。
        // 读写有内存屏障
        //   volatile 读有 acquire 语义，后面的读写不会被重排到它前面
        //   volatile 写有 release 语义，前面的读写不会被重排到它后面。
        public volatile bool Loop = true;
        public void Deposit()
        {
            Console.WriteLine("loop start");
            while (Loop)
            {
                Balance += 10;
            }
            Console.WriteLine("loop end");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("main thread start");

            Person person = new Person();
            ThreadStart threadStart = new ThreadStart(person.Deposit);
            Thread thread = new Thread(threadStart);
            thread.Start();
            Thread.Sleep(500);
            person.Loop = false;

            Console.WriteLine("main thread end");
        }
    }
}