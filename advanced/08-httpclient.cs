// 主题：HttpClient —— 发送 HTTP 请求（代替已过时的 WebRequest / WebResponse，SYSLIB0014）
using System.Diagnostics;

namespace HelloWorld
{
    internal class HttpHelper
    {
        // 整个程序共用一个 HttpClient，不要每次请求都 new（否则会耗尽端口）
        private static readonly HttpClient httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)       // 默认 100 秒
        };

        public async Task StartAsync(string url)
        {
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                // ResponseHeadersRead：收到响应头就返回，正文稍后再读；
                // 默认会等正文全部下载完才返回，那样"请求耗时"和"读取耗时"就分不开了
                using HttpResponseMessage response =
                    await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                Console.WriteLine($"状态码: {(int)response.StatusCode}, 请求耗时: {stopwatch.ElapsedMilliseconds}ms");
                response.EnsureSuccessStatusCode();     // 不是 2xx 就抛 HttpRequestException

                stopwatch.Restart();
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"读取耗时: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"字符串长度: {content.Length}");

                // 只要字符串时可以一步到位：await httpClient.GetStringAsync(url);
                // JSON：using System.Net.Http.Json; await httpClient.GetFromJsonAsync<T>(url);（需要 PublishAot=false）
            }
            catch (HttpRequestException ex)            // 网络错误、非 2xx 状态码
            {
                Console.WriteLine($"请求失败: {ex.StatusCode} {ex.Message}");
            }
            catch (TaskCanceledException)              // 超时
            {
                Console.WriteLine("请求超时");
            }
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("main thread start");

            HttpHelper httpHelper = new HttpHelper();
            await httpHelper.StartAsync("https://www.baidu.com");   // 必须 await，否则 Main 先结束

            Console.WriteLine("main thread end");
        }
    }
}
