#:property PublishAot=false
using System.Text.Json;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Print();
            // Print("Yin", "Owen", 38);
            Print(lastName: "Owen", age: 38);
        }

        static void Print(string firstName = "", string lastName = "", int age = 18)
        {
            Console.WriteLine($"name: {lastName} {firstName}, age: {age}");

        }
    }
}