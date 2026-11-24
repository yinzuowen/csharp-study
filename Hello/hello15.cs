#:property PublishAot=false
using System.Diagnostics.CodeAnalysis;

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal class Program
    {
        static void Main(string[] args)
        {
            // Person person = new Person();
            // Console.WriteLine(person.Birthday);
            // person.Age = 18;
            // Console.WriteLine(person.Age);

            Person person = new Person(38);
            Console.WriteLine(person.Age);
        }
    }

    class Person
    {
        public DateTime Birthday { get; private set; }
        public required int Age { get; init; }

        [SetsRequiredMembers]
        public Person()
        {
            Birthday = DateTime.Now;
            Age = 38;
        }

        [SetsRequiredMembers]
        public Person(int age)
        {
            Age = age;
        }

        public void SetBirthday(DateTime birthday)
        {
            Birthday = birthday;
        }
    }
}