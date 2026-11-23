// 主题：Dictionary<TKey, TValue> —— 对应 Java 的 HashMap；取值用 TryGetValue，dict[key] 取不存在的 key 会抛 KeyNotFoundException

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();

            dict.Add("Alice", 18);
            dict.Add("Bob", 20);

            if (dict.TryGetValue("Alice", out int age))
            {
                Console.WriteLine($"Alice: {age}");
            }

            if (dict.ContainsKey("Bob"))
            {
                Console.WriteLine("Bob exists");
            }

            foreach (KeyValuePair<string, int> entry in dict)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value}");
            }

            dict["Chris"] = 22;                      // 索引器赋值：key 不存在就添加，存在就覆盖（dict.Add 重复 key 会抛异常）

            try
            {
                Console.WriteLine(dict["David"]);    // 索引器取值：key 不存在抛异常（Java 的 map.get 返回 null）
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }

            dict.Remove("Alice");
            Console.WriteLine("--- 删除 Alice 后 ---");

            foreach (KeyValuePair<string, int> entry in dict)
            {
                Console.WriteLine($"{entry.Key}: {entry.Value}");
            }
        }
    }
}