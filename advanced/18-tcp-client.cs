// 主题：TCP 客户端（逐行发送给服务器；输入 Q 退出）
using System.Net.Sockets;   // TcpClient、NetworkStream
using System.Text;          // Encoding

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("客户端已启动，正在连接...");
            // 构造函数里就会连接服务器；服务器没启动时抛 SocketException（Connection refused）
            TcpClient client = new TcpClient("localhost", 12345);
            Console.WriteLine("已连接，输入消息后回车发送（Q 退出）：");
            NetworkStream stream = client.GetStream();
            while (true)
            {
                string? message = Console.ReadLine();   // 输入结束（Ctrl+D）时返回 null
                if (message is null || message.Equals("Q", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                else
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                    Console.WriteLine("已发送消息：" + message);
                }
            }
            client.Close();
            Console.WriteLine("客户端已关闭");
        }
    }
}