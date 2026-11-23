// 主题：Queue<T> —— 队列，先进先出：Enqueue 入队，Dequeue 出队，Peek 只看不取

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Queue<int> queue = new Queue<int>();

            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);

            Console.WriteLine(string.Join(", ", queue));

            Console.WriteLine($"Peek: {queue.Peek()}");      // 只看队头，不移除
            Console.WriteLine($"Dequeue: {queue.Dequeue()}"); // 取出并移除队头

            Console.WriteLine(string.Join(", ", queue));
        }
    }
}