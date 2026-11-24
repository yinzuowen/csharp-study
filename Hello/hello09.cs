#:property PublishAot=false
using System.Text.Json;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            double perimeter = GetPerimeter(2);
            Console.WriteLine(perimeter);
        }

        static double GetPerimeter(double diameter, double PI = 3.1415926)
        {
            return 2 * PI * diameter;
        }
    }
}