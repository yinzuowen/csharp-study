#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal class Program
    {
        static void Main(string[] args)
        {
            Person person = new Person();
            // person.SetName("Owen");
            // Console.WriteLine(person.GetName());
            person.Name = "Owen";
            Console.WriteLine(person.Name);
        }
    }

    class Person
    {
        private string name = string.Empty;

        public string GetName()
        {
            return name;
        }

        public bool SetName(string value)
        {
            name = value;
            return true;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}