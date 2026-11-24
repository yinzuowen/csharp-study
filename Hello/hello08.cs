#:property PublishAot=false
using System.Text.Json;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Union(null));
            Console.WriteLine(Union([]));
            Console.WriteLine(Union("Tom", "Jerry"));
        }

        static string Union(params string[] array)
        {
            if (array == null)
            {
                return "array is null";
            }

            if (array.Length == 0)
            {
                return "array.length is 0";
            }

            string result = string.Empty;

            foreach (var item in array)
            {
                result += $"{item}, ";
            }

            return result;
        }
    }
}