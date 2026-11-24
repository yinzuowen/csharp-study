namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Person person = new Person("Owen", 36);

            // PlayMovie();
            // GC.Collect();

            Movie movie = new Movie(DateTime.Now);
            movie.Film = "Jerry";
                Console.WriteLine($"Film: {movie.Film}");

            string film = "Tom";
            int money = movie.Buy(film);
            if (money > 0)
            {
                Console.WriteLine($"{film}: {money}");
            }
        }

        static void PlayMovie()
        {
            Movie movie = new Movie(DateTime.Now);
            Console.WriteLine($"影片上映时间{movie.dateTime.ToString("hh:mm:ss")}");
        }
    }

    internal class Movie
    {
        public DateTime dateTime;
        private string[] list = ["Tom", "Jerry"];
        private string? film;

        public Movie(DateTime dateTime)
        {
            this.dateTime = dateTime;
        }

        ~Movie()
        {
            Console.WriteLine($"影片撤档时间{DateTime.Now.ToString("hh:mm:ss")}");
        }

        public string Film
        {
            get { return film; }
            set { film = value; }
        }

        public int Buy(string name)
        {
            int index = list.ToList().IndexOf(name);

            if (index > -1)
            {
                film = name;
                return index + 10;
            }

            return 0;
        }

        public void Play()
        {
            Console.WriteLine($"正在播放:{film}");
        }
    }

    internal class Person
    {
        public string Name = "Owen";
        private int age = 36;
        public static int money = 0;

        static Person()
        {
            // 静态构造函数只执行一次，用于初始化静态成员
        }

        public Person(string name, int age)
        {
            this.Name = name;
            this.age = age;
        }
    }
}