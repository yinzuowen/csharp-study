// 主题：HashSet<T> —— 不重复的集合，对应 Java 的 HashSet；Add 重复元素返回 false，不会报错

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            HashSet<int> set = new HashSet<int>();

            set.Add(1);
            set.Add(2);
            set.Add(3);
            bool added = set.Add(2);                 // 重复元素：不报错，返回 false
            set.Add(3);
            set.Add(4);

            Console.WriteLine($"再次添加 2：{added}");

            Console.WriteLine(string.Join(", ", set));

            set.Remove(2);

            Console.WriteLine(string.Join(", ", set));
        }
    }
}