// 主题：对象初始化器、运算符重载

// ---------- 两种创建方式 ----------
Movie movie1 = new Movie("Owen");                 // 构造函数传参
Movie movie2 = new Movie { Film = "Yin" };        // 对象初始化器：先 new，再给属性赋值
Movie movie0 = new() { Film = "Tom" };            // 左边写了类型，右边可以只写 new()

// ---------- 运算符重载：movie1 + movie2 会调用 operator + ----------
Movie movie3 = movie1 + movie2;
Console.WriteLine(movie3.Film);                   // Owen, Yin
Console.WriteLine((movie3 + movie0).Film);        // Owen, Yin, Tom

Console.WriteLine(movie1 == new Movie("Owen"));   // True：重载了 ==，按片名比较
Console.WriteLine(movie1 == movie2);              // False

class Movie
{
    public Movie() { }

    public Movie(string film)
    {
        Film = film;
    }

    // 自动属性 + 默认值，避免 CS8618 警告
    public string Film { get; set; } = string.Empty;

    // 运算符重载：必须是 public static，参数里至少有一个是当前类型
    public static Movie operator +(Movie left, Movie right)
        => new Movie { Film = left.Film + ", " + right.Film };

    // == 和 != 必须成对重载，同时建议重写 Equals 和 GetHashCode
    public static bool operator ==(Movie? left, Movie? right) => left?.Film == right?.Film;
    public static bool operator !=(Movie? left, Movie? right) => !(left == right);
    public override bool Equals(object? obj) => obj is Movie m && m.Film == Film;
    public override int GetHashCode() => Film.GetHashCode();
}
