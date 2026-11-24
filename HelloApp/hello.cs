namespace Hello
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var person = new Person { Birthday = new DateTime(1988, 5, 20) };

            int age = DateTime.Today.Year - person.Birthday.Year;
            if (DateTime.Today < person.Birthday.AddYears(age)) age--;   // 今年生日还没到

            Console.WriteLine($"生日：{person.Birthday:yyyy-MM-dd}");
            Console.WriteLine($"年龄：{age}");
        }
    }
}
