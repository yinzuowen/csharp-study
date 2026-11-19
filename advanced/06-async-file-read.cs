// 主题：async/await 读文件 —— 代替 06-apm-begin-read.cs 的 BeginRead / EndRead / Callback
using System.Text;

namespace HelloWorld
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string fileName = Path.Combine(AppContext.BaseDirectory, "async-demo.txt");
            await File.WriteAllTextAsync(fileName, "Hello async\n你好，async");

            // ---------- 写法 1：整个文件读成字符串（最常用，默认 UTF-8） ----------
            string content = await File.ReadAllTextAsync(fileName);
            Console.WriteLine($"ReadAllTextAsync:\n{content}");

            Console.WriteLine("--------------------------");

            // ---------- 写法 2：需要控制流的时候 ----------
            // await using：用完自动关闭，即使中途抛异常也会关
            await using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read,
                                                 FileShare.Read, 4096, useAsync: true))
            {
                byte[] buffer = new byte[fs.Length];
                Console.WriteLine($"await 前：线程 {Environment.CurrentManagedThreadId}");
                int count = await fs.ReadAsync(buffer);
                Console.WriteLine($"await 后：线程 {Environment.CurrentManagedThreadId}（可能换了线程）");
                Console.WriteLine($"ReadAsync, length {count}:\n{Encoding.UTF8.GetString(buffer, 0, count)}");
            }

            // useAsync: true —— 让 ReadAsync 走操作系统的真异步 I/O，等待期间不占线程（Windows 上效果明显）
            // 用 File.ReadAllTextAsync 这类方法时不用管，.NET 内部已经设置好了
        }
    }
}
