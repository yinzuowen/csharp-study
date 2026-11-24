#:property PublishAot=false
using System.Text.Json;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Line line = new Line();
            line.Length = 50;
            Console.WriteLine($"Main line.Length = {line.Length}");
            // Update1(line);
            // Update2(line);
            Update3(ref line);
            Console.WriteLine($"Main line.Length = {line.Length}");
        }

        static void Update1(Line line)
        {
            Console.WriteLine($"Before Update: {line.Length}");
            line = new Line();
            line.Length = 100;
            Console.WriteLine($"After Update: {line.Length}");
        }

        static void Update2(Line line)
        {
            Console.WriteLine($"Before Update: {line.Length}");
            line.Length = 100;
            Console.WriteLine($"After Update: {line.Length}");
        }

        static void Update3(ref Line line)
        {
            Console.WriteLine($"Before Update: {line.Length}");
            line = new Line();
            line.Length = 100;
            Console.WriteLine($"After Update: {line.Length}");
        }
    }

    internal class Line
    {
        public int Length { get; set; }
    }
}