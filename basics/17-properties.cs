// 主题：属性 vs Get/Set 方法、只读属性、计算属性

// ---------- Java 风格：Get/Set 方法 ----------
var p1 = new Person();
p1.SetName("Owen");
Console.WriteLine(p1.GetName());

// ---------- C# 风格：属性，用起来像字段 ----------
p1.Name = "Owen";
Console.WriteLine(p1.Name);

// ---------- 只读的计算属性：只有 get，值由其他字段算出来 ----------
var p2 = new FullNamePerson("Owen", "Yin");
Console.WriteLine(p2.FullName);
Console.WriteLine(p2.Initials);
// p2.FullName = "Zuowen Yin";   // ❌ 只有 get，不能赋值

// ---------- { get; } = 和 => 的区别：只算一次 vs 每次访问都重新算 ----------
var login = new LoginInfo();
Console.WriteLine($"Birthday={login.Birthday:HH:mm:ss.fff}, LoginTime={login.LoginTime:HH:mm:ss.fff}");
Thread.Sleep(1000);
Console.WriteLine($"Birthday={login.Birthday:HH:mm:ss.fff}, LoginTime={login.LoginTime:HH:mm:ss.fff}");   // 只有 LoginTime 变了

class Person
{
    private string name = string.Empty;

    // Java 风格的读写方法
    public string GetName() => name;
    public void SetName(string value) => name = value;

    // C# 属性：编译后其实就是 get_Name() 和 set_Name(value) 两个方法
    public string Name
    {
        get { return name; }
        set { name = value; }
    }
}

class FullNamePerson
{
    private readonly string firstName;
    private readonly string lastName;

    public FullNamePerson(string firstName, string lastName)
    {
        this.firstName = firstName;
        this.lastName = lastName;
    }

    // 只读计算属性（完整写法）
    public string FullName
    {
        get { return $"Full Name: {lastName} {firstName}"; }
    }

    // 简写：表达式体属性，效果一样
    public string Initials => $"{firstName[0]}.{lastName[0]}.";
}

class LoginInfo
{
    public DateTime Birthday { get; } = DateTime.Now;   // 自动属性 + 初始值：创建对象时算一次，存进隐藏字段
    public DateTime LoginTime => DateTime.Now;          // 表达式体属性：没有字段，每次读取都执行一次
}
