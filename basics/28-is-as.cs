// 主题：类型检查与转换 —— is、is 模式匹配、as、强制转换
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- is：判断对象是不是某个类型（或它的子类） ----------
var animal = new Animal();
Console.WriteLine($"animal is Animal: {animal is Animal}");
Console.WriteLine($"animal is Cat: {animal is Cat}");

var cat = new Cat();
Console.WriteLine($"cat is Animal: {cat is Animal}");
Console.WriteLine($"cat is Cat: {cat is Cat}");

Console.WriteLine("--------------------------");

// ---------- is 模式匹配：判断 + 转换 + 声明变量，一步完成（推荐） ----------
object obj = new Cat();
if (obj is Animal a)                  // a 是新变量，类型是 Animal；只在判断成立时有值
{
    Console.WriteLine($"GetType().FullName: {a.GetType().FullName}");   // GetType() 返回实际类型：Cat
    a.Shout();                        // 多态：Meow
}

// 反过来写：不是就提前返回，之后 c 一定有值
if (obj is not Cat c) return;
c.Shout();

Console.WriteLine("--------------------------");

// ---------- as：转换失败返回 null，不抛异常，所以结果类型要声明成可空 ----------
Cat? cat2 = obj as Cat;
if (cat2 != null)
{
    Console.WriteLine($"as 成功：{cat2}");
}

string? str = obj as string;          // 失败 → null
Console.WriteLine($"as string: {str ?? "null"}");
Console.WriteLine($"?. 安全调用：{str?.Length}");

// ---------- 强制转换：确定类型时用，失败抛 InvalidCastException ----------
Cat cat3 = (Cat)obj;
Console.WriteLine($"(Cat)obj: {cat3}");
try
{
    _ = (string)obj;                  // _ 表示丢弃结果
}
catch (InvalidCastException ex)
{
    Console.WriteLine($"强转失败：{ex.GetType().Name}");
}

// as 不能用于值类型：obj as int 报错，可以写 obj as int?
object number = 42;
int? n = number as int?;
Console.WriteLine($"as int?: {n}");

class Animal
{
    public string Name { get; set; } = string.Empty;

    public virtual void Shout() => Console.WriteLine(Name);
}

class Cat : Animal
{
    public override void Shout() => Console.WriteLine("Meow");
}
