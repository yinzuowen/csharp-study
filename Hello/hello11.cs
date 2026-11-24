#:property PublishAot=false

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Person person = new Person();
            person.Birthday = DateTime.Parse("1988-12-28");
            Console.WriteLine(person.Birthday);
            Console.WriteLine(person.GetAge());
        }
    }

    public static class PersonExtension
    {
        // 扩展方法
        public static double GetAge(this Person person)
        {
            return Math.Ceiling((DateTime.Now - person.Birthday).TotalDays / 365);
        }
    }

    public sealed class Person
    {
        public DateTime Birthday { get; set; }
    }
}