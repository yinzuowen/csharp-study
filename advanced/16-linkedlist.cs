// 主题：LinkedList<T> —— 双向链表，AddLast / AddAfter 返回 LinkedListNode，拿到节点后插入、删除都是 O(1)

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            LinkedList<int> linkedList = new LinkedList<int>();

            linkedList.AddLast(1);
            var node2 = linkedList.AddLast(2);
            linkedList.AddLast(3);
            linkedList.AddAfter(node2, 4);

            Console.WriteLine(string.Join(", ", linkedList));

            linkedList.Remove(node2);                // 直接按节点删除，不用从头查找

            Console.WriteLine(string.Join(", ", linkedList));
        }
    }
}