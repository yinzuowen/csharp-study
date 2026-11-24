#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal delegate void CalculateDelegate(int a, int b);

    internal class Program
    {
        static void Main(string[] args)
        {
            Person person = new Person();

            Console.WriteLine($"Birthday={person.Birthday}");
            Console.WriteLine($"LoginTime={person.LoginTime}");

            Thread.Sleep(1000);

            Console.WriteLine($"Birthday={person.Birthday}");
            Console.WriteLine($"LoginTime={person.LoginTime}");

            int[] numbers = [1, 2, 3, 4, 5];

            // Select 返回的不是数组，而是 IEnumerable
            int[] results = numbers.Select(item => item * item).ToArray();

            foreach (var item in results)
            {
                Console.Write(item + ", ");
            }
        }
    }

    internal class Person
    {
        public DateTime Birthday { get; } = DateTime.Now;
        public DateTime LoginTime => DateTime.Now;
    }
}