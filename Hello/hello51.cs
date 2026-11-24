#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal interface IInput
    {
        string Content { set; get; }

        void Input(string content);
    }

    internal abstract class Inputer : IInput
    {
        public abstract void Input(string content);
    }

    internal static class Extension
    {
        public static void Show(this IInput input)
        {
            Console.WriteLine(input.Content);
        }
    }

    internal class Reader : IInput
    {
        public string Content { set; get; } = string.Empty;

        public void Input(string content)
        {
            Content = content;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Reader reader = new Reader();
            reader.Input("Hello World");
            reader.Show();
        }
    }
}