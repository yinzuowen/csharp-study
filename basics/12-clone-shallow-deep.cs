#:property PublishAot=false
// 主题：浅拷贝 vs 深拷贝
// （单文件程序默认开启 AOT，会禁用反射式 JSON，所以第一行把 AOT 关掉）
using System.Text.Json;

Movie movie = new Movie { Id = 1, Film = "Titanic" };
Console.WriteLine("原对象：");
movie.Show();

// ---------- 浅拷贝：MemberwiseClone ----------
// 值类型和 string 是独立的；数组等引用类型只复制了引用，两个对象共用同一个数组
Movie shallow = (Movie)movie.Clone();
Update(shallow);
Console.WriteLine("\n浅拷贝并修改副本后：");
Console.Write("副本  "); shallow.Show();
Console.Write("原对象"); movie.Show();       // Audiences 被副本改了！

// ---------- 深拷贝：序列化成 JSON 再反序列化 ----------
// （BinaryFormatter 因安全问题已在 .NET 9 中移除，改用 System.Text.Json）
movie = new Movie { Id = 1, Film = "Titanic" };
Movie deep = CloneDeep(movie);
Update(deep);
Console.WriteLine("\n深拷贝并修改副本后：");
Console.Write("副本  "); deep.Show();
Console.Write("原对象"); movie.Show();       // 完全不受影响

static void Update(Movie movie)
{
    movie.Id = 2;
    movie.Film = "xxxx";
    movie.Audiences[0] = "Dick";
    movie.Audiences[1] = "Emma";
    movie.Audiences[2] = "Frank";
}

static T CloneDeep<T>(T obj)
{
    string json = JsonSerializer.Serialize(obj);
    return JsonSerializer.Deserialize<T>(json)!;   // ! 告诉编译器结果不是 null
}

class Movie : ICloneable
{
    public int Id { get; set; }
    public string Film { get; set; } = string.Empty;
    public string[] Audiences { get; set; } = ["Alice", "Bob", "Chris"];

    // 浅拷贝
    public object Clone() => MemberwiseClone();

    // 手写深拷贝的写法：数组单独复制一份
    // public object Clone()
    // {
    //     var copy = (Movie)MemberwiseClone();
    //     copy.Audiences = (string[])Audiences.Clone();
    //     return copy;
    // }

    public void Show() => Console.WriteLine($"id={Id}, film={Film}, audiences={string.Join(" ", Audiences)}");
}
