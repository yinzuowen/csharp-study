// 主题：枚举、Enum 常用 API、[Flags]
// enum 继承自 System.Enum，默认底层类型是 int

Color c = Color.Red;
Console.WriteLine(c);

Type colorType = typeof(Color);
Console.WriteLine($"BaseType: {colorType.BaseType}");                          // System.Enum
Console.WriteLine($"GetUnderlyingType(): {Enum.GetUnderlyingType(colorType)}"); // System.Int32
Console.WriteLine($"GetName(1): {Enum.GetName((Color)1)}");                     // Green

Console.WriteLine($"GetNames: {string.Join(", ", Enum.GetNames<Color>())}");
foreach (var value in Enum.GetValues<Color>())
    Console.Write($"{value}={(int)value} ");
Console.WriteLine();

// 字符串 → 枚举
Color red = Enum.Parse<Color>("Red");                      // 失败抛异常
Console.WriteLine($"Parse: {red}");
if (Enum.TryParse<Color>("Purple", out Color colorResult)) // 失败返回 false
    Console.WriteLine($"TryParse: {colorResult}");
else
    Console.WriteLine("TryParse: cannot convert Purple");

// 数字 → 枚举
Console.WriteLine($"(Color)2: {(Color)2}");

// ---------- [Flags]：可以按位组合 ----------
for (int j = 0; j < 8; j++)
    Console.WriteLine("{0,3} - {1:G}", j, (MultiRole)j);   // {0,3} 右对齐占 3 格

var role = MultiRole.Delete | MultiRole.Select;
Console.WriteLine($"role = {role}, HasFlag(Select) = {role.HasFlag(MultiRole.Select)}");

enum Color { Red, Green, Blue }

[Flags]
enum MultiRole
{
    Insert = 0,   // 注意：Flags 里 0 表示"一个都没有"，Insert 无法和其他值组合
    Delete = 1,   // 推荐写法：None = 0, Insert = 1, Delete = 2, Update = 4, Select = 8
    Update = 2,
    Select = 4
}
