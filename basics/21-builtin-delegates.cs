// 主题：内置委托 Action / Func / Predicate —— 绝大多数情况下不用自己定义 delegate
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

int[] numbers = [1, 2, 3, 4, 5];

// ---------- Action<参数...>：没有返回值 ----------
Action action1 = () => Console.WriteLine("Hello World");   // 0 个参数
action1.Invoke();

Action<string> action2 = Greet;                            // 1 个参数
action2("Owen");

Action<int, int> action3 = PrintSum;                       // 2 个参数
action3(1, 2);

Action<int> printParity = number =>
{
    Console.WriteLine($"{number}: {(number % 2 == 0 ? "even" : "odd")}");
};
foreach (var number in numbers)
{
    printParity(number);
}

Console.WriteLine("--------------------------");

// ---------- Func<参数..., 返回值>：最后一个类型参数是返回值 ----------
Func<int, int, int> add = Add;
Console.WriteLine($"add(2, 3) = {add(2, 3)}");

// 委托放进集合，配合 LINQ 的 Select（Select 的参数本身就是 Func<T, TResult>）
Func<int, int> square = x => x * x;
Func<int, int> cube = x => x * x * x;
var handlers = new List<Func<int, int>> { square, cube };
foreach (var handler in handlers)
{
    Console.WriteLine(string.Join(", ", numbers.Select(handler)));
}

// 委托的参数也可以是委托：Func<Func<int>, string>
Func<Func<int>, string> check = Check;
Console.WriteLine(check(() => 3));

Console.WriteLine("--------------------------");

// ---------- Predicate<T>：接收 T，返回 bool（等价于 Func<T, bool>） ----------
Predicate<int> isEven = IsEven;
foreach (var number in numbers)
{
    // 插值字符串里的三元运算符要加括号，否则 : 会被当成格式说明符
    Console.WriteLine($"{number} is {(isEven(number) ? "even" : "odd")}");
}
Console.WriteLine($"第一个偶数：{Array.Find(numbers, isEven)}");   // 很多 API 直接收 Predicate

static void Greet(string name) => Console.WriteLine($"Hello {name}");

static void PrintSum(int a, int b) => Console.WriteLine($"{a} + {b} = {a + b}");

static int Add(int a, int b) => a + b;

static string Check(Func<int> func)
{
    int result = func();
    string parity = result % 2 == 0 ? "even" : "odd";
    return $"{result} is {parity}";
}

static bool IsEven(int n) => n % 2 == 0;

// 注意：不要自己定义名叫 Action / Func 的委托，会遮蔽 System.Action，导致上面的 Action<string> 报错
