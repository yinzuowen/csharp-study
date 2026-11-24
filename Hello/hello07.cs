#:property PublishAot=false
using System.Text.Json;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int result = Square(-2, out string message);
            Console.WriteLine($"area: {result}, message: {message}");
            result = Square(2, out message);
            Console.WriteLine($"area: {result}, message: {message}");
        }

        static int Square(int length, out string message)
        {
            if (length > 0)
            {
                message = "length is OK";
                return length * length;
            }

            message = "length should be greater than 0";
            return length;
        }
    }
}