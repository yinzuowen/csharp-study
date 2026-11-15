// 主题：构造函数、静态构造函数、this、: base(...) 继承、终结器（析构函数）

// ---------- 构造函数 + 继承 ----------
var person = new Person("Owen", 36);
person.Show();

var male = new Male("Tom", 20);      // 先执行 Person 的构造函数，再执行 Male 的
male.Show();

// ---------- 属性 + 方法 ----------
Movie movie = new Movie(DateTime.Now);
Console.WriteLine($"影片上映时间 {movie.DateTime:HH:mm:ss}");   // HH 是 24 小时制，hh 是 12 小时制

movie.Film = "Jerry";
Console.WriteLine($"Film: {movie.Film}");

string film = "Tom";
int money = movie.Buy(film);
if (money > 0)
    Console.WriteLine($"{film}: {money}");
movie.Play();

// ---------- 终结器 ----------
// 终结器由 GC 在另一个线程上调用，时间不确定；
// .NET Core 起程序退出时也不会执行终结器，所以要手动触发 GC 并等待才能看到输出
PlayMovie();                    // 方法结束后，里面的 Movie 对象就没人引用了
GC.Collect();
GC.WaitForPendingFinalizers();

// 放在单独的方法里：Debug 模式下局部变量会一直存活到所在方法结束，
// 如果直接在这里 new，对象要到程序结束才能被回收
static void PlayMovie()
{
    var temp = new Movie(DateTime.Now);
    Console.WriteLine($"临时影片上映时间 {temp.DateTime:HH:mm:ss}");
}

class Person
{
    public static int Count;          // 静态字段
    public string Name;
    private int age;                  // 和参数同名，所以赋值时必须写 this.age

    // 静态构造函数：第一次使用这个类时自动执行，只执行一次
    static Person()
    {
        Count = 0;
        Console.WriteLine("Person 静态构造函数执行");
    }

    public Person(string name, int age)
    {
        Name = name;                  // 大小写不同，不需要 this
        this.age = age;               // 同名，需要 this 区分字段和参数
        Count++;
    }

    public void Show() => Console.WriteLine($"{GetType().Name}: {Name}, {age} 岁, 已创建 {Count} 个");
}

class Male : Person
{
    // 父类没有无参构造函数，子类必须用 : base(...) 把参数传过去（相当于 Java 的 super）
    public Male(string name, int age) : base(name, age)
    {
        Console.WriteLine("Male 构造函数执行");
    }
}

class Movie
{
    private readonly string[] _list = ["Tom", "Jerry"];
    private string? _film;            // 没买票时没有影片，所以是可空的

    public Movie(DateTime dateTime)
    {
        DateTime = dateTime;
    }

    // 终结器：对象被垃圾回收时调用，实际开发很少写（释放资源用 IDisposable + using）
    ~Movie()
    {
        Console.WriteLine($"影片撤档时间 {DateTime.Now:HH:mm:ss}");
    }

    public DateTime DateTime { get; }

    public string? Film
    {
        get { return _film; }
        set { _film = value; }
    }

    public int Buy(string name)
    {
        int index = Array.IndexOf(_list, name);   // 不需要 ToList()
        if (index > -1)
        {
            _film = name;
            return index + 10;
        }
        return 0;
    }

    public void Play() => Console.WriteLine($"正在播放: {_film}");
}
