// 主题：const、readonly、static、@ 关键字转义

const int days = 365;          // 局部常量
// days = 123;                 // ❌ 不能修改
Console.WriteLine($"days = {days}");

Console.WriteLine($"MyMath.PI  = {MyMath.PI}");           // const：编译时确定，自动是 static
Console.WriteLine($"MyMath.PI2 = {MyMath.PI2}");          // static readonly：静态构造函数里赋值
Console.WriteLine($"System.Math.PI = {Math.PI}");         // 系统的 Math，没有被我们的类盖住

var m = new MyMath(2.718);
Console.WriteLine($"m.E = {m.E}");                        // 实例 readonly：普通构造函数里赋值

// static 字段（不是常量）：属于类，可以修改
Console.WriteLine($"Counter.Count = {Counter.Count}");
Counter.Count = 10;
Console.WriteLine($"Counter.Count = {Counter.Count}");

// @ 可以把关键字当变量名用
float @class = 0;
Console.WriteLine($"@class = {@class}");

// 类名叫 MyMath 而不是 Math，避免盖住 System.Math
class MyMath
{
    // const 只能是内置类型或 string，object 不能用 const
    public const double PI = 3.1415926;
    public const char Cr = '\r';
    public const string Hello = "hello";

    // static readonly：只能在声明时或静态构造函数里赋值
    public static readonly double PI2;
    static MyMath() { PI2 = 3.1415926; }

    // 实例 readonly：只能在声明时或普通构造函数里赋值
    public readonly double E = 0;
    public MyMath(double e) { E = e; }

    // public void Change() { E = 1; }   // ❌ 构造函数以外不能改
}

class Counter
{
    public static int Count = 0;
}
