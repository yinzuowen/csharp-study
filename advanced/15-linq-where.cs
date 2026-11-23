// 主题：LINQ 查询语法入门 —— from / where / select，结果是 IEnumerable<T>

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int[] numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

            IEnumerable<int> collection = from number in numbers where number % 2 == 0 select number;

            foreach (int item in collection)
            {
                Console.WriteLine(item);
            }
        }
    }
}