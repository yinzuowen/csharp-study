// 主题：APM 模式异步读文件 —— BeginRead / EndRead + AsyncCallback（老写法，了解即可）
//
// Stream.BeginRead / EndRead 在现代 .NET 里还保留着（委托的 BeginInvoke 才被去掉）
using System.Text;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string fileName = Path.Combine(AppContext.BaseDirectory, "apm-demo.txt");
            File.WriteAllText(fileName, "Hello APM\n你好，APM");    // 准备一个 UTF-8 文件

            using var done = new ManualResetEvent(false);           // 回调执行完后点亮
            var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read,   // 只读：Open + Read
                                            FileShare.Read, 4096, useAsync: true);      // useAsync：用操作系统的异步 I/O
            byte[] buffer = new byte[fileStream.Length];

            Console.WriteLine($"start reading at {DateTime.Now:HH:mm:ss}");
            fileStream.BeginRead(buffer, 0, buffer.Length, ar =>
            {
                try
                {
                    int count = fileStream.EndRead(ar);              // 每个 BeginXxx 都要配一个 EndXxx：拿结果、抛异常
                    Console.WriteLine($"end reading, length: {count}");
                    // 只解码实际读到的部分；文件是 UTF-8，用 Encoding.Unicode（UTF-16）会乱码
                    Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, count));
                }
                finally
                {
                    fileStream.Close();                              // 读完再关
                    done.Set();                                      // 通知主线程
                }
            }, null);

            Console.WriteLine("main thread is free to do other work...");
            done.WaitOne();                                          // 不等的话 Main 结束，回调可能来不及执行
            Console.WriteLine("main end");
        }
    }
}
