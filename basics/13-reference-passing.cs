// 主题：引用类型作为参数传递的三种情况
// 引用类型变量里存的是"地址"，传参时默认复制的是地址（值传递）

Line line = new Line { Length = 50 };

// 1. 方法里 new 一个新对象：只改了方法内的局部变量，外面不受影响
Update1(line);
Console.WriteLine($"Update1 之后 Main: {line.Length}\n");   // 50

// 2. 方法里修改对象的属性：两边指向同一个对象，外面跟着变
Update2(line);
Console.WriteLine($"Update2 之后 Main: {line.Length}\n");   // 100

// 3. ref 传递 + new 新对象：外面的变量也指向了新对象
line.Length = 50;
Update3(ref line);
Console.WriteLine($"Update3 之后 Main: {line.Length}");     // 200

static void Update1(Line line)
{
    Console.WriteLine($"Update1 Before: {line.Length}");
    line = new Line { Length = 100 };          // 局部变量指向了新对象
    Console.WriteLine($"Update1 After:  {line.Length}");
}

static void Update2(Line line)
{
    Console.WriteLine($"Update2 Before: {line.Length}");
    line.Length = 100;                         // 修改的是共享的那个对象
    Console.WriteLine($"Update2 After:  {line.Length}");
}

static void Update3(ref Line line)
{
    Console.WriteLine($"Update3 Before: {line.Length}");
    line = new Line { Length = 200 };          // ref：连外面的变量一起改
    Console.WriteLine($"Update3 After:  {line.Length}");
}

class Line
{
    public int Length { get; set; }
}
