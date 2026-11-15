// 主题：数组、复制、LINQ（GroupBy / Select / SelectMany）、交错数组

// int[] array1;
// Console.WriteLine(array1.Length);   // ❌ 编译错误：使用了未赋值的局部变量

int[] array2 = [1, 2, 3, 4, 5];
Console.WriteLine($"Length = {array2.Length}");
Console.WriteLine($"LastOrDefault = {array2.LastOrDefault()}");
Console.WriteLine($"LastOrDefault(>10, -1) = {array2.LastOrDefault(n => n > 10, -1)}");

// ---------- 引用 vs 复制 ----------
var array3 = array2;             // 只复制引用
Console.WriteLine($"array2 == array3: {array2 == array3}");   // True

var cloned = array2.ToArray();   // 复制一份新数组（或 (int[])array2.Clone()）
Console.WriteLine($"array2 == cloned: {array2 == cloned}");   // False
Array.ForEach(cloned, item => Console.Write($"{item} "));
Console.WriteLine();

// ---------- GroupBy ----------
foreach (var g in array2.GroupBy(item => item % 2 == 0))
    Console.WriteLine($"Key = {g.Key}: {string.Join(", ", g)}");

// ---------- Select vs SelectMany ----------
int[][] selected = array2.Select(item => new[] { item, item * 10 }).ToArray();
Console.WriteLine("Select（数组的数组）:");
foreach (var p in selected)
    Console.WriteLine($"  [{string.Join(", ", p)}]");

int[] selectedMany = array2.SelectMany(item => new[] { item, item * 10 }).ToArray();
Console.WriteLine($"SelectMany（拍平）: {string.Join(", ", selectedMany)}");

// ---------- 交错数组：每行长度可以不同 ----------
int[][] jagged = new int[5][];
jagged[0] = [1];
jagged[1] = [2, 3];
jagged[2] = [4, 5, 6];
jagged[3] = [7, 8, 9, 10];
jagged[4] = [11, 12, 13, 14, 15];

for (int i = 0; i < jagged.Length; i++)
{
    for (int j = 0; j < jagged[i].Length; j++)
        Console.Write($"{jagged[i][j]} ");
    Console.WriteLine();
}
