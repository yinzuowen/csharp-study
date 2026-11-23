// 主题：group ... into —— 把分组结果起个名字，继续对分组做 where / select（相当于 SQL 的 having）

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int[] ages = [10, 20, 30, 40, 17, 24, 36, 23, 33, 38, 39];

            var collection = from age in ages
                             group age by age / 10 into Items
                             where Items.Count() > 1
                             select new { Count = Items.Count(), Items };

            foreach (var item in collection)
            {
                Console.WriteLine($"{item.Count}: {string.Join(',', item.Items)}");
            }
        }
    }
}