// 主题：方法参数的几种形式（out、params、可选参数、命名参数）

// ---------- out：通过参数"多返回"一个值 ----------
// out 参数在方法里必须赋值；调用时可以直接声明变量 out string message
int result = Square(-2, out string message);
Console.WriteLine($"area: {result}, message: {message}");
result = Square(2, out message);
Console.WriteLine($"area: {result}, message: {message}");

// 系统里最常见的 out：TryParse
if (int.TryParse("123", out int n))
    Console.WriteLine($"TryParse: {n}");

// ---------- params：可变数量的参数（相当于 Java 的 String...） ----------
Console.WriteLine(Union(null));             // 传 null
Console.WriteLine(Union());                 // 不传 → 空数组
Console.WriteLine(Union("Tom", "Jerry"));   // 直接写多个参数，编译器自动打包成数组
Console.WriteLine(Union(["Tom", "Jerry"])); // 也可以直接传数组

// ---------- 可选参数：有默认值的参数可以不传 ----------
Console.WriteLine(GetPerimeter(2));              // PI 用默认值
Console.WriteLine(GetPerimeter(2, Math.PI));     // 传入更精确的 PI

// ---------- 命名参数：按名字传参，可以不按顺序、可以跳过中间的可选参数 ----------
Print();
Print("Owen", "Yin", 38);
Print(lastName: "Yin", age: 38);   // 跳过 firstName
Print(age: 20, firstName: "Tom");  // 顺序随意

static int Square(int length, out string message)
{
    if (length > 0)
    {
        message = "length is OK";
        return length * length;
    }

    message = "length should be greater than 0";
    return length;
}

// string[]? 表示允许传 null；params 必须是最后一个参数
static string Union(params string[]? array)
{
    if (array is null) return "array is null";
    if (array.Length == 0) return "array.length is 0";
    return string.Join(", ", array);
}

// 可选参数必须放在必选参数后面，默认值必须是编译时常量
static double GetPerimeter(double diameter, double pi = 3.1415926)
{
    return pi * diameter;   // 周长 = π × 直径
}

static void Print(string firstName = "", string lastName = "", int age = 18)
{
    Console.WriteLine($"name: {lastName} {firstName}, age: {age}");
}
