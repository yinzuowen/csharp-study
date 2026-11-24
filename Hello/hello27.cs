#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 事件
    internal class Button
    {
        public event EventHandler? Click;

        public void Start()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Button button = new Button();
            button.Click += Button_Click; // 订阅
            button.Click -= Button_Click; // 取消订阅
            button.Click += new EventHandler(Button_Click);
            button.Click += (sender, args) => Console.WriteLine($"{sender}, ${args}");
            button.Click += delegate (object? sender, EventArgs args)
            {
                Console.WriteLine($"{sender}, ${args}");
            };
            button.Start();
        }

        static void Button_Click(object? sender, EventArgs args)
        {
            Console.WriteLine($"{sender}, ${args}");
        }
    }


}