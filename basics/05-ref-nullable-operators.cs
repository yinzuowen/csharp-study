#:property AllowUnsafeBlocks=true
// 主题：ref 局部变量、可空类型、指针、is / as / sizeof / typeof

// ---------- ref：给变量起别名 ----------
int a = 100;
int b = a;          // b 是复制
ref int c = ref a;  // c 是 a 的别名，指向同一个变量
Console.WriteLine($"a={a}, b={b}, c={c}");
a = 200; Console.WriteLine($"a=200 后: a={a}, b={b}, c={c}");   // c 跟着变
b = 300; Console.WriteLine($"b=300 后: a={a}, b={b}, c={c}");   // 只有 b 变
c = 400; Console.WriteLine($"c=400 后: a={a}, b={b}, c={c}");   // a 跟着变

// ---------- 可空值类型 + ?? ----------
int? x = null;
int y = x ?? 100;   // x 为 null 时用 100
Console.WriteLine($"y={y}");

// ---------- 指针：需要 unsafe + AllowUnsafeBlocks ----------
int n = 123;
unsafe
{
    int* ptr = &n;   // & 取地址
    Console.WriteLine($"*ptr={*ptr}");               // * 取值
    Console.WriteLine($"ptr={(long)ptr:X}");         // 地址（十六进制）
}

// ---------- is / as ----------
if (n is int) Console.WriteLine("n is int");   // 编译器会警告 CS0183：n 本来就是 int，结果永远是 true

object obj = "hello";
string? text = obj as string;            // 转换失败返回 null，不抛异常
Console.WriteLine($"text={text}");

if (obj is string s)                     // 判断 + 转换 + 赋值 一步完成
    Console.WriteLine($"长度 {s.Length}");

// ---------- sizeof / typeof：只能用于类型 ----------
Console.WriteLine($"sizeof(int)={sizeof(int)}");
Console.WriteLine($"typeof(int)={typeof(int)}");
Console.WriteLine($"obj.GetType()={obj.GetType()}");   // 变量用 GetType()
