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
            Person person = new Person("Yin", "Owen");
            Console.WriteLine(person.FullName);
            // person.FullName = "Zuowen Yin";
            // Console.WriteLine(person.FullName);
        }
    }

    class Person
    {
        private string firstName;
        private string lastName;

        public string FullName
        {
            get
            {
                return $"Full Name: {lastName} {firstName}";
            }
        }

        public Person(string firstName, string lastName)
        {
            this.firstName = firstName;
            this.lastName = lastName;
        }
    }
}