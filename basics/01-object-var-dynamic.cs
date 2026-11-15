#:property PublishAot=false
// 主题：字符串拼接、object、var、dynamic

// ---------- 字符串拼接的几种方式 ----------
string firstName = "Owen", lastName = "Yin";
Console.WriteLine(firstName + lastName);                 // + 拼接

char[] chars = ['O', 'w', 'e', 'n'];                     // char 用单引号
Console.WriteLine(new string(chars));                    // char[] → string

string[] parts = ["Owen", "Yin"];
Console.WriteLine(string.Join(" ", parts));              // 用分隔符连接

// ---------- object：所有类型的基类 ----------
object o1 = new object();
object o2 = new object();
object o3 = o1;
Console.WriteLine($"o1 == o2: {Equals(o1, o2)}");        // False，不同对象
Console.WriteLine($"o1 == o3: {Equals(o1, o3)}");        // True，同一个对象
Console.WriteLine($"o1.GetHashCode(): {o1.GetHashCode()}");
Console.WriteLine($"o2.GetHashCode(): {o2.GetHashCode()}");
Console.WriteLine($"o1.GetType(): {o1.GetType()}");

// ---------- object / dynamic / var ----------
object o = 99;                      // 变量类型是 object，可以装任何值
Console.WriteLine($"o.GetType(): {o.GetType()}");   // 实际类型 System.Int32
o = "test";
Console.WriteLine($"o.GetType(): {o.GetType()}");   // System.String

dynamic d = 99;                     // 运行时才确定类型，编译器不检查
Console.WriteLine($"d.GetType(): {d.GetType()}");
d = "test";
Console.WriteLine($"d.Length: {d.Length}");          // 运行时才去找 Length

var v = 1;                          // 编译时推断为 int，之后类型固定
Console.WriteLine($"v.GetType(): {v.GetType()}");
// v = "test";                      // ❌ 编译错误：v 已经是 int
