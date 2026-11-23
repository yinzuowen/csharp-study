// 主题：Stack<T> —— 栈，后进先出：Push 入栈，Pop 出栈，Peek 只看不取；foreach 从栈顶开始遍历

namespace HelloWorld
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Stack<int> stack = new Stack<int>();

            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            Console.WriteLine(string.Join(", ", stack));

            Console.WriteLine($"Peek: {stack.Peek()}");      // 只看栈顶，不移除
            Console.WriteLine($"Pop: {stack.Pop()}");        // 取出并移除栈顶

            Console.WriteLine(string.Join(", ", stack));
        }
    }
}