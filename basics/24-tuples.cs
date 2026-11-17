// 主题：元组 —— Tuple（老，引用类型） vs 值元组 (a, b)（新，值类型，推荐）
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- System.Tuple：class，只读，成员名只能是 Item1、Item2… ----------
var obj1 = Tuple.Create(1, 2, 3);
Console.WriteLine($"{obj1}, Item1={obj1.Item1}");

var obj2 = Tuple.Create("Owen", 38);
Console.WriteLine(obj2);

// ---------- 值元组 ValueTuple：struct，可读写，可以给成员起名 ----------
(string, int) obj3 = ("Zoe", 10);
Console.WriteLine($"{obj3}, Item1={obj3.Item1}");

(string Name, int Age) obj4 = ("Zoe", 10);
obj4.Age++;
Console.WriteLine($"Name={obj4.Name}, Age={obj4.Age}");

// ---------- 最常见用途：方法返回多个值（代替 out 参数） ----------
List<int> list = [1, 2, 4, 8, 16];
var result = MinMax(list);
Console.WriteLine($"min={result.min}, max={result.max}");

// 解构：直接拆成两个变量
var (min, max) = MinMax(list);
Console.WriteLine($"min={min}, max={max}");

// 交换两个变量，不需要临时变量
int a = 1, b = 2;
(a, b) = (b, a);
Console.WriteLine($"a={a}, b={b}");

static (int min, int max) MinMax(List<int> list)
{
    int min = int.MaxValue;
    int max = int.MinValue;

    foreach (var item in list)
    {
        if (item < min) min = item;
        if (item > max) max = item;
    }

    return (min, max);
}
