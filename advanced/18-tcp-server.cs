// 主题：TCP 服务器（一直运行，每个客户端一个 Task，可同时服务多个客户端；Ctrl+C 退出）
using System.Net.Sockets;   // TcpListener、TcpClient、NetworkStream
using System.Text;          // Encoding

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            TcpListener listener = TcpListener.Create(12345);
            listener.Start();
            Console.WriteLine("服务器已启动，等待连接...（Ctrl+C 退出）");

            while (true)    // 服务器一直运行，不断接受新连接
            {
                TcpClient client = listener.AcceptTcpClient();   // 阻塞，直到有客户端连进来
                // 每个客户端交给一个 Task 处理，主线程立刻回去 Accept 下一个
                // 如果在这里直接读，处理第一个客户端时，第二个客户端就连不上了
                Task.Run(() => HandleClient(client));
            }
        }

        static void HandleClient(TcpClient client)
        {
            string who = client.Client.RemoteEndPoint?.ToString() ?? "未知";
            Console.WriteLine($"[{who}] 已连接");
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;
                // Read 返回 0 表示客户端正常关闭了连接
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    // 直接解码 buffer 的前 bytesRead 个字节，不需要先 Array.Copy
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[{who}] 收到：{receivedData}");
                }
            }
            catch (IOException)
            {
                // 客户端异常断开（比如进程被杀）时，Read 会抛 IOException
                // 只结束这个客户端，不能让它影响整个服务器
            }
            finally
            {
                client.Close();
            }
            Console.WriteLine($"[{who}] 已断开");
        }
    }
}
