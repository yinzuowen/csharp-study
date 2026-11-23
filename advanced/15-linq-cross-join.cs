// 主题：两个独立集合的 from —— 笛卡尔积，每个元素两两组合（相当于 SQL 的 cross join）

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] firsts = ["洪七公", "郭靖", "黄蓉"];
            string[] seconds = ["欧阳锋", "欧阳克", "杨康"];

            var collection = from first in firsts
                             from second in seconds
                             select (first, second);

            foreach (var (first, second) in collection)
            {
                Console.WriteLine($"{first} vs {second}");
            }
        }
    }
}