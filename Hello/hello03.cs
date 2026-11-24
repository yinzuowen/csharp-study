namespace HelloWorld
{
    internal class Program
    {
        static void Main(string[] args)
        {

            // Movie movie1 = new Movie("Owen");
            // Movie movie2 = new Movie("Yin");

            Movie movie1 = new Movie() { Film = "Owen" };
            Movie movie2 = new Movie() { Film = "Yin" };

            Movie movie3 = movie1 + movie2;
            Console.WriteLine(movie3.Film);
        }
    }

    internal class Movie
    {
        private string[] list = ["Tom", "Jerry"];
        private string film = string.Empty;

        public Movie()
        {

        }

        public Movie(string film)
        {
            Film = film;
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

        // 操作符重载
        public static Movie operator +(Movie left, Movie right)
        {
            Movie movie = new Movie();
            movie.Film = left.Film + ", " + right.Film;
            return movie;
        }

    }
}