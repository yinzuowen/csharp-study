// 主题：orderby —— 升序 ascending（默认）/ 降序 descending，排序后再分组

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int[] ages = [10, 20, 30, 40, 17, 24, 36, 23, 33, 38, 39];

            var collection = from age in ages
                             orderby age ascending
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