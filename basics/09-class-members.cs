// 主题：类的成员一览、属性（完整写法 vs 自动属性）、构造函数

var p1 = new Person();
var p2 = new Person("Owen") { Age = 38 };   // 构造函数 + 对象初始化器
Console.WriteLine($"{p2.Name}, {p2.Age}");

var demo = new ClassName();
demo[0] = "zero";                            // 索引器
Console.WriteLine($"demo[0] = {demo[0]}");
demo.NameChanged += (sender, e) => Console.WriteLine("事件触发：PropertyName 变了");
demo.PropertyName = "new value";

// 类可以包含的成员种类
class ClassName
{
    public const int ConstName = 123;                // 常量
    private string _fieldName = string.Empty;        // 字段
    private readonly string[] _items = new string[10];

    public ClassName() { }                           // 构造函数
    ~ClassName() { }                                 // 终结器（析构函数），实际很少用

    public string PropertyName                       // 属性
    {
        get => _fieldName;
        set
        {
            _fieldName = value;
            NameChanged?.Invoke(this, EventArgs.Empty);   // 触发事件
        }
    }

    public string this[int i]                        // 索引器：让对象能用 obj[i] 访问
    {
        get => _items[i];
        set => _items[i] = value;
    }

    public void MethodName(string args) { }          // 方法

    public event EventHandler? NameChanged;          // 事件
}

class Person
{
    private string name = string.Empty;   // 字段

    public Person()
    {
        Console.WriteLine("Hello, I'm (no name)");
    }

    public Person(string name)
    {
        Name = name;
        Console.WriteLine($"Hello, I'm {name}");
    }

    // 属性完整写法：字段 + get/set，set 里的 value 是赋过来的值
    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    // 自动属性：没有额外逻辑时一行搞定，效果和上面一样
    public int Age { get; set; }
}
