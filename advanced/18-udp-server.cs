// 主题：UDP 服务器（收到消息后回复；收到 Q 时退出）
using System.Net;           // IPEndPoint、IPAddress
using System.Net.Sockets;   // UdpClient
using System.Text;          // Encoding

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            UdpClient listener = new UdpClient(12000);
            // 用来接收“发送方是谁”，Receive 会通过 ref 把它改成实际的发送方地址
            IPEndPoint clientIPEndpoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                Console.WriteLine("等待接收...");
                while (true)
                {
                    byte[] bytes = listener.Receive(ref clientIPEndpoint);   // 阻塞，直到收到一个数据报
                    string received = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                    Console.WriteLine($"收到{clientIPEndpoint}的消息：{received}");

                    byte[] sendBytes = Encoding.UTF8.GetBytes("服务器收到消息");
                    listener.Send(sendBytes, sendBytes.Length, clientIPEndpoint);   // 回复给发送方

                    if (received.Equals("Q", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);   // 相当于 Java 的 ex.printStackTrace()
            }
            finally
            {
                listener.Close();
            }
        }
    }
}
