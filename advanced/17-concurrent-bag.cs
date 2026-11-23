// 主题：ConcurrentBag —— 线程安全的无序集合，适合多个线程同时往里添加结果；配合 Parallel.For 使用

using System.Collections.Concurrent;   // 并发集合不在隐式 using 里，要手动引入

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            int[] array = [1, 2, 3, 4, 5];

            var bag = new ConcurrentBag<int>();

            Parallel.For(0, array.Length, i =>
            {
                bag.Add(array[i]);
            });

            foreach (var number in bag)
            {
                Console.WriteLine($"number = {number}");
            }
        }
    }
}