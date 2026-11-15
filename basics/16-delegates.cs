// 主题：委托（delegate）—— "方法的类型"，可以把方法当作变量和参数传递
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- 三种给委托赋值的写法 ----------
Multiple m1 = Multiply;                                     // 1. 普通方法
Multiple m2 = delegate (int a, int b) { return a * b; };    // 2. 匿名方法（C# 2.0 的老写法）
Multiple m3 = (a, b) => a * b;                              // 3. lambda（现在的主流写法）

Console.WriteLine($"2 * 3 = {m1(2, 3)}");
Console.WriteLine($"2 * 3 = {m2(2, 3)}");
Console.WriteLine($"2 * 3 = {m3(2, 3)}");

// ---------- 内置委托：一般不用自己定义 delegate ----------
Func<int, int, int> add = (a, b) => a + b;     // Func<参数..., 返回值>
Action<string> say = msg => Console.WriteLine(msg);   // Action<参数...>：没有返回值
Predicate<int> isEven = n => n % 2 == 0;       // Predicate<T>：返回 bool

Console.WriteLine($"2 + 3 = {add(2, 3)}");
say("hello from Action");
Console.WriteLine($"isEven(4) = {isEven(4)}");

// ---------- 委托作为参数：把"做什么"传进方法 ----------
Console.WriteLine($"Calc(6, 3, +) = {Calc(6, 3, (a, b) => a + b)}");
Console.WriteLine($"Calc(6, 3, -) = {Calc(6, 3, (a, b) => a - b)}");
Console.WriteLine($"Calc(6, 3, *) = {Calc(6, 3, Multiply)}");

// ---------- 多播：一个委托可以挂多个方法 ----------
Action log = () => Console.WriteLine("第 1 个");
log += () => Console.WriteLine("第 2 个");
log();

static int Multiply(int a, int b) => a * b;

static int Calc(int a, int b, Func<int, int, int> op) => op(a, b);

// 自定义委托类型：规定"接收两个 int、返回 int"的方法签名
delegate int Multiple(int a, int b);
