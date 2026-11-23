// 主题：SortedDictionary<TKey, TValue> —— 按 key 排序的字典，对应 Java 的 TreeMap；可以传入 IComparer 自定义排序

namespace HelloWorld
{
    // 降序比较器：返回负数表示 x 排在 y 前面
    // 不传比较器时默认按 key 升序；写成 x.CompareTo(y) 就和默认一样，看不出效果
    internal class DescendingComparer : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return y.CompareTo(x);
        }
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            SortedDictionary<int, string> dict = new SortedDictionary<int, string>(new DescendingComparer());
            // 也可以不写类，直接用 lambda：Comparer<int>.Create((x, y) => y.CompareTo(x))

            dict.Add(20, "Bob");
            dict.Add(18, "Alice");
            dict.Add(25, "Chris");

            foreach (KeyValuePair<int, string> entry in dict)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value}");
            }
        }
    }
}