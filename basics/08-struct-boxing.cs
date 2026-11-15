// 主题：struct（值类型）、ValueType、值传递 vs ref 传递、装箱拆箱
//
// 值类型的基类是 System.ValueType，ValueType 的基类是 System.Object
// 引用类型（class、string、数组）直接继承 System.Object

Point point = new Point(100, 200);
Write(point);      // 调用重写的 ToString()
Write(point.X);
Write(point.Y);
// Write(point.ToString());   // ❌ string 是引用类型，不能传给 ValueType 参数

// ---------- 值传递：方法里改的是副本 ----------
DoSomethingByValue(point);
Console.Write("值传递之后：");
Write(point);      // 没变

// ---------- ref 传递：方法里改的是原变量 ----------
DoSomethingByRef(ref point);
Console.Write("ref 传递之后：");
Write(point);      // 变了

// ---------- struct 赋值是复制 ----------
var p1 = new Point(1, 1);
var p2 = p1;
p2.X = 99;
Console.WriteLine($"p1={p1}, p2={p2}");   // p1 不受影响

// ---------- 装箱 / 拆箱 ----------
int num = 123;
object boxed = num;          // 装箱：值类型 → object，复制到堆上
int unboxed = (int)boxed;    // 拆箱：object → 值类型
Console.WriteLine($"boxed={boxed}, unboxed={unboxed}");

static void Write(ValueType param) => Console.WriteLine(param);

static void DoSomethingByValue(Point point)
{
    point.X = 300;
    point.Y = 400;
    Console.Write("方法内（值传递）：");
    Write(point);
}

static void DoSomethingByRef(ref Point point)
{
    point.X = 300;
    point.Y = 400;
    Console.Write("方法内（ref）：");
    Write(point);
}

struct Point
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override readonly string ToString() => $"Point(X={X}, Y={Y})";
}
