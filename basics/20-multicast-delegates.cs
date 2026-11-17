// 主题：自定义委托的调用方式与多播（+= / -=）
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- 两种调用方式：直接调用 和 .Invoke() 完全等价 ----------
HelloWorldDelegate hello = Show;
hello();
hello.Invoke();

HelloWorldDelegate2 greet = Greet;
greet("Owen");

Console.WriteLine("--------------------------");

// ---------- 多播：+= 挂多个方法，调用时按添加顺序依次执行 ----------
// new CalculateDelegate(Add) 是老写法，和 CalculateDelegate calc = Add; 一样
CalculateDelegate? calc = new CalculateDelegate(Add);   // -= 可能把它减成 null，所以声明成可空
calc += Sub;
calc?.Invoke(2, 3);

Console.WriteLine("--------------------------");

// ---------- -= 移除 ----------
calc -= Add;
calc?.Invoke(2, 3);   // 只剩 Sub；全部移除后 calc 就是 null，所以用 ?.

// 同一个方法可以挂多次，会执行多次
hello += Show;
Console.WriteLine("--------------------------");
hello();              // Hello World 打印两次

static void Show() => Console.WriteLine("Hello World");

static void Greet(string name) => Console.WriteLine($"Hello {name}");

static void Add(int a, int b) => Console.WriteLine($"{a} + {b} = {a + b}");

static void Sub(int a, int b) => Console.WriteLine($"{a} - {b} = {a - b}");

// 委托声明：方法签名前面加 delegate，方法名的位置就是新类型的名字
delegate void HelloWorldDelegate();
delegate void HelloWorldDelegate2(string name);
delegate void CalculateDelegate(int a, int b);
