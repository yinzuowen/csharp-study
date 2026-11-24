#:property PublishAot=false
using System.Text.Json;

namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Movie movie = new Movie();
            movie.Id = 1;
            movie.Film = "Titanic";
            movie.Show();

            Movie other = CloneDeep(movie);
            Update(other);
            other.Show();

            movie.Show();

        }

        static void Update(Movie movie)
        {
            movie.Id = 2;
            movie.Film = "xxxx";
            movie.Audiences[0] = "Dick";
            movie.Audiences[1] = "Emma";
            movie.Audiences[2] = "Frank";
        }

        // 深拷贝：先序列化成 JSON 字符串，再反序列化成一个全新的对象
        // （BinaryFormatter 因安全问题已在 .NET 9 中移除，改用 System.Text.Json）
        static T CloneDeep<T>(T obj)
        {
            string json = JsonSerializer.Serialize(obj);
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }

    internal class Movie : ICloneable
    {
        public int Id { get; set; }
        public string Film { get; set; } = string.Empty;
        public string[] Audiences { get; set; } = ["Alice", "Bob", "Chris"];

        // 浅拷贝
        public object Clone()
        {
            return MemberwiseClone();
        }

        public void Show()
        {
            Console.WriteLine($"id={Id}, film={Film}");
            Console.Write("Audience: ");
            foreach (var item in Audiences)
            {
                Console.Write($"{item} ");
            }
            Console.WriteLine();
        }
    }
}