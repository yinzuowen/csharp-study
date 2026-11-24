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

            Person person = new Person(-18);

            try
            {
                Check(person);
            }
            catch (DataEntityException<Person> ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void Check(Person person)
        {
            if (person.Age < 0)
            {
                throw new DataEntityException<Person>(person, "age must be greater than 0");
            }
        }
    }

    class Person
    {
        public int Age { get; init; } = 0;
        public Person(int age)
        {
            Age = age;
        }
    }

    class DataEntityException<T> : Exception
    {
        public T Entity { get; }

        // : base(message) 把消息交给父类 Exception，这样 ex.Message 才能拿到它
        public DataEntityException(T entity, string message) : base(message)
        {
            Entity = entity;
        }
    }
}