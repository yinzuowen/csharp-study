// 主题：UDP 客户端（发送一行，等待服务器回复；输入 Q 退出）
using System.Net;           // IPEndPoint、IPAddress
using System.Net.Sockets;   // UdpClient
using System.Text;          // Encoding

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            UdpClient client = new UdpClient();   // 不指定端口，系统随机分配一个
            IPEndPoint serverIPEndpoint = new IPEndPoint(IPAddress.Loopback, 12000);
            // 用来接收“发送方是谁”，Receive 会通过 ref 把它改成实际的发送方地址
            IPEndPoint remoteIPEndpoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                while (true)
                {
                    string? message = Console.ReadLine();
                    if (message is null) break;   // 输入结束（Ctrl+D）

                    byte[] sendBytes = Encoding.UTF8.GetBytes(message);
                    client.Send(sendBytes, sendBytes.Length, serverIPEndpoint);
                    Console.WriteLine($"发送：{message}");

                    byte[] bytes = client.Receive(ref remoteIPEndpoint);
                    string received = Encoding.UTF8.GetString(bytes);

                    Console.WriteLine($"收到{remoteIPEndpoint}的响应：{received}");

                    // 判断的是自己发出去的 message：服务器的回复永远是“服务器收到消息”，不会是 Q
                    if (message.Equals("Q", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                client.Close();
            }
        }
    }
}
