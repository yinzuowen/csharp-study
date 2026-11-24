# 第 7 章 集合与 LINQ 🟢

> 本章练习：
> - 数组与 LINQ 方法语法：[06-arrays-linq.cs](../basics/06-arrays-linq.cs)
> - 常用集合：[16-dictionary](../advanced/16-dictionary.cs)、[16-hashset](../advanced/16-hashset.cs)、[16-sorted-dictionary](../advanced/16-sorted-dictionary.cs)、[16-linkedlist](../advanced/16-linkedlist.cs)、[16-queue](../advanced/16-queue.cs)、[16-stack](../advanced/16-stack.cs)
> - LINQ 查询语法：[15-linq-where](../advanced/15-linq-where.cs)、[15-linq-where-multiple](../advanced/15-linq-where-multiple.cs)、[15-linq-compound-from](../advanced/15-linq-compound-from.cs)、[15-linq-cross-join](../advanced/15-linq-cross-join.cs)、[15-linq-select-anonymous](../advanced/15-linq-select-anonymous.cs)、[15-linq-select-method-call](../advanced/15-linq-select-method-call.cs)、[15-linq-orderby](../advanced/15-linq-orderby.cs)、[15-linq-group-by](../advanced/15-linq-group-by.cs)、[15-linq-group-into](../advanced/15-linq-group-into.cs)、[15-linq-inner-join](../advanced/15-linq-inner-join.cs)、[15-linq-group-join](../advanced/15-linq-group-join.cs)、[15-linq-left-join](../advanced/15-linq-left-join.cs)、[15-linq-let](../advanced/15-linq-let.cs)、[15-linq-distinct](../advanced/15-linq-distinct.cs)

> 写代码时你会遇到这些问题：
> 从 `Dictionary` 里取一个不存在的 key，程序直接崩了（Java 的 `HashMap.get` 只是返回 `null`）；`foreach` 里删元素报 `Collection was modified`；一个 LINQ 查询明明只写了一次，里面的代码却执行了好几遍；`First()` 抛 `Sequence contains no elements`；用 `ToDictionary` 时报 key 重复。
> 本章讲数组、常用集合类、集合接口层次，以及 LINQ（相当于 Java 8 的 Stream，但 C# 早就有了，而且用得更多）。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。

## 1. 数组

### 1.1 一维数组

```csharp
int[] a = new int[3];        // 元素是默认值 0
int[] b = { 1, 2, 3 };       // 老写法，和 Java 一样
int[] c = [4, 5, 6];         // 集合表达式（C# 12），推荐
Console.WriteLine(string.Join(",", a));
Console.WriteLine($"{b.Length} {c[^1]}");          // ^1 = 倒数第一个
Console.WriteLine(string.Join(",", c[0..2]));      // 范围：下标 0、1，不含 2
```
```
0,0,0
3 6
4,5
```

- 长度是 `Length`（属性，不是 Java 的 `length` 字段，也不是方法）。
- 越界抛 `IndexOutOfRangeException`（Java 是 `ArrayIndexOutOfBoundsException`）。
- `^1`、`0..2` 是 Java 里没有的语法：`^n` 表示倒数第 n 个，`a..b` 是左闭右开区间，返回一个**新数组**。
- 常用静态方法在 `Array` 类上：`Array.Sort`、`Array.IndexOf`、`Array.ForEach`、`Array.Reverse`，相当于 Java 的 `Arrays`。

### 1.2 引用与复制

数组是引用类型。练习 06 里：

```csharp
int[] array2 = [1, 2, 3, 4, 5];
var array3 = array2;             // 只复制引用
var cloned = array2.ToArray();   // 复制一份新数组（或 (int[])array2.Clone()）
Console.WriteLine($"array2 == array3: {array2 == array3}");
Console.WriteLine($"array2 == cloned: {array2 == cloned}");
```
```
array2 == array3: True
array2 == cloned: False
```

`==` 对数组比较的是引用。要比较内容用 `array2.SequenceEqual(cloned)`（LINQ）。

### 1.3 多维数组 vs 交错数组

C# 有两种"二维数组"，Java 只有后一种：

| | 多维数组（矩形） | 交错数组（数组的数组） |
|---|---|---|
| 写法 | `int[,] grid = new int[2, 3];` | `int[][] jagged = new int[5][];` |
| 访问 | `grid[1, 2]` | `jagged[1][2]` |
| 每行长度 | 必须相同 | 可以不同 |
| 行数 / 列数 | `GetLength(0)` / `GetLength(1)` | `jagged.Length` / `jagged[i].Length` |
| Java 对应 | 没有 | `int[][]` |

```csharp
int[,] grid = new int[2, 3];
grid[1, 2] = 9;
Console.WriteLine($"Rank={grid.Rank} Length={grid.Length} rows={grid.GetLength(0)} cols={grid.GetLength(1)}");

int[][] jagged = [[1], [2, 3], [4, 5, 6]];
Console.WriteLine($"jagged.Length={jagged.Length} jagged[2].Length={jagged[2].Length}");
```
```
Rank=2 Length=6 rows=2 cols=3
jagged.Length=3 jagged[2].Length=3
```

⚠️ 多维数组的 `Length` 是**元素总数**（2×3=6），不是行数。

练习 06 里的交错数组每行长度不同：

```
1 
2 3 
4 5 6 
7 8 9 10 
11 12 13 14 15 
```

实际写业务时数组用得不多，**大部分场景用 `List<T>`**。

## 2. 常用集合 🟢

### 2.1 速查表

都在 `System.Collections.Generic` 下（已经隐式 using，不用自己写）。

| C# | Java 对应 | 特点 |
|---|---|---|
| `List<T>` | `ArrayList<T>` | 最常用，下标访问 O(1) |
| `Dictionary<K,V>` | `HashMap<K,V>` | 键值对，无序 |
| `SortedDictionary<K,V>` | `TreeMap<K,V>` | 按 key 排序 |
| `HashSet<T>` | `HashSet<T>` | 去重，无序 |
| `SortedSet<T>` | `TreeSet<T>` | 去重且排序 |
| `Queue<T>` | `ArrayDeque` 当队列用 | 先进先出 |
| `Stack<T>` | `Stack` / `ArrayDeque` 当栈用 | 后进先出 |
| `LinkedList<T>` | `LinkedList<T>` | 双向链表，很少用 |
| `PriorityQueue<T,P>` | `PriorityQueue<T>` | 优先级单独传 |

方法名的差异：

| 操作 | C# | Java |
|---|---|---|
| 元素个数 | `Count`（属性） | `size()` |
| 数组长度 | `Length` | `length` |
| 是否包含 | `Contains` / `ContainsKey` | `contains` / `containsKey` |
| 加一批 | `AddRange` | `addAll` |
| 按条件删 | `RemoveAll(n => ...)` | `removeIf` |
| 入队 / 出队 | `Enqueue` / `Dequeue` | `offer` / `poll` |
| 入栈 / 出栈 | `Push` / `Pop` | `push` / `pop` |

### 2.2 List&lt;T&gt;

```csharp
var list = new List<int> { 5, 3, 1 };
list.Add(4); list.AddRange([9, 8]); list.Insert(0, 100); list.Remove(3); list.RemoveAt(0);
list.Sort();
Console.WriteLine($"{string.Join(",", list)} Count={list.Count} Contains(9)={list.Contains(9)} IndexOf(8)={list.IndexOf(8)}");
Console.WriteLine(list.Find(n => n > 4));          // 第一个满足条件的
Console.WriteLine(list.RemoveAll(n => n % 2 == 0)); // 返回删了几个
Console.WriteLine(string.Join(",", list));
```
```
1,4,5,8,9 Count=5 Contains(9)=True IndexOf(8)=3
5
2
1,5,9
```

- `Remove(3)` 是按**值**删，`RemoveAt(0)` 是按**下标**删。Java 的 `List<Integer>.remove(int)` 有装箱歧义，C# 分成两个方法名，没有这个坑。
- `List<int>` 里直接存 `int`，没有装箱（Java 只能写 `List<Integer>`）。

### 2.3 Dictionary&lt;K,V&gt; ⚠️ 和 HashMap 最大的不同

```csharp
var ages = new Dictionary<string, int>
{
    ["Tom"] = 18,
    ["Zoe"] = 10,
};
ages["Amy"] = 20;          // 索引器赋值：不存在就添加
ages["Tom"] = 19;          // 存在就覆盖
try { ages.Add("Tom", 30); }
catch (ArgumentException ex) { Console.WriteLine($"Add 重复: {ex.GetType().Name}: {ex.Message}"); }
try { Console.WriteLine(ages["Bob"]); }
catch (KeyNotFoundException ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }
```
```
Add 重复: ArgumentException: An item with the same key has already been added. Key: Tom
KeyNotFoundException: The given key 'Bob' was not present in the dictionary.
```

**`dict[key]` 取不存在的 key 会抛 `KeyNotFoundException`**，不会像 Java `HashMap.get` 那样返回 `null`。（而且 `int` 是值类型，本来也没法返回 `null`。）

安全取值的几种写法：

```csharp
if (ages.TryGetValue("Bob", out int bobAge)) Console.WriteLine(bobAge);
else Console.WriteLine("Bob 不存在");
Console.WriteLine(ages.GetValueOrDefault("Bob", -1));   // 相当于 Java 的 getOrDefault
Console.WriteLine(ages.TryAdd("Tom", 99));              // 已存在返回 false，不抛异常
Console.WriteLine(ages.ContainsKey("Zoe"));
Console.WriteLine(ages.Remove("Zoe"));
foreach (var (name, age) in ages) Console.WriteLine($"{name}={age}");   // 解构遍历
```
```
Bob 不存在
-1
False
True
True
Tom=19
Amy=20
```

| 操作 | C# `Dictionary` | Java `HashMap` |
|---|---|---|
| 取不存在的 key | `dict[k]` 抛 `KeyNotFoundException` | `get(k)` 返回 `null` |
| 安全取值 | `TryGetValue(k, out var v)` | `get` 再判 `null` |
| 带默认值 | `GetValueOrDefault(k, d)` | `getOrDefault(k, d)` |
| 添加，key 已存在 | `Add` 抛 `ArgumentException`；`dict[k] = v` 覆盖；`TryAdd` 返回 false | `put` 覆盖；`putIfAbsent` |
| 遍历 | `foreach (var (k, v) in dict)` 或 `KeyValuePair<K,V>` | `entrySet()` |

**推荐：取值用 `TryGetValue`**，一次查找就同时判断了存在并拿到值。先 `ContainsKey` 再 `dict[k]` 要查两次。

### 2.4 其他集合

```csharp
var sorted = new SortedDictionary<string, int> { ["b"] = 2, ["c"] = 3, ["a"] = 1 };
Console.WriteLine(string.Join(", ", sorted));

var set = new HashSet<int> { 1, 2, 3 };
Console.WriteLine(set.Add(2));           // 已存在，返回 false
set.UnionWith([3, 4]);                   // 并集，还有 IntersectWith、ExceptWith
Console.WriteLine(string.Join(",", set));

var q = new Queue<string>();
q.Enqueue("a"); q.Enqueue("b");
Console.WriteLine($"{q.Peek()} {q.Dequeue()} {q.Count}");
Console.WriteLine(q.TryDequeue(out var x) ? x : "空");
try { q.Dequeue(); } catch (InvalidOperationException ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }

var st = new Stack<int>();
st.Push(1); st.Push(2);
Console.WriteLine($"{st.Peek()} {st.Pop()} {st.Count}");

var ll = new LinkedList<int>([1, 2, 3]);
ll.AddFirst(0); ll.AddLast(4);
ll.AddAfter(ll.Find(2)!, 99);
Console.WriteLine(string.Join(",", ll));
```
```
[a, 1], [b, 2], [c, 3]
False
1,2,3,4
a a 1
b
InvalidOperationException: Queue empty.
2 2 1
0,1,2,99,3,4
```

⚠️ 空队列 `Dequeue()`、空栈 `Pop()` 抛 `InvalidOperationException`（Java 的 `poll()` 返回 `null`）。不确定是否为空时用 `TryDequeue` / `TryPop`。

### 2.5 集合初始化器

```csharp
var list = new List<int> { 1, 2, 3 };                    // 集合初始化器（调用 Add）
var dict = new Dictionary<string, int> { ["a"] = 1 };    // 索引初始化器
List<int> list2 = [1, 2, 3];                             // 集合表达式（C# 12）
int[] merged = [.. list, .. list2, 4];                   // .. 展开，类似 JS 的 ...
```

Java 1.6 里要先 `new` 再一行行 `add`，或者用 `Arrays.asList`。C# 里这些写法都很常见。

## 3. 集合接口与只读集合

### 3.1 接口层次

```
IEnumerable<T>          只能 foreach（LINQ 都是它的扩展方法）
  └─ ICollection<T>     + Count、Add、Remove、Contains
       └─ IList<T>      + 下标 [i]、Insert、IndexOf

IReadOnlyCollection<T>  只读 + Count
  └─ IReadOnlyList<T>   只读 + 下标 [i]
```

| C# | Java 对应 | 用在哪 |
|---|---|---|
| `IEnumerable<T>` | `Iterable<T>` | 参数：只需要遍历 |
| `ICollection<T>` | `Collection<T>` | 很少直接用 |
| `IList<T>` | `List<T>`（接口） | 需要下标和修改 |
| `IReadOnlyList<T>` | 无（只有 `Collections.unmodifiableList`） | 返回值：不让调用方改 |
| `IDictionary<K,V>` / `IReadOnlyDictionary<K,V>` | `Map<K,V>` | |

⚠️ 名字的坑：C# 的 `List<T>` 是**类**（对应 Java 的 `ArrayList`），C# 的 `IList<T>` 才是接口（对应 Java 的 `List`）。

经验：**参数类型尽量宽（`IEnumerable<T>`），返回类型尽量具体但只读（`IReadOnlyList<T>`）。**

### 3.2 只读集合

```csharp
var list = new List<int> { 1, 2, 3 };
IReadOnlyList<int> ro = list;
// ro.Add(4);   // 编译错误：IReadOnlyList 没有 Add
Console.WriteLine(ro[0]);
list.Add(4);
Console.WriteLine($"ro.Count={ro.Count}");     // 只是只读"视图"，底层变了它也能看到

var wrapper = list.AsReadOnly();               // 真正的只读包装
Console.WriteLine(wrapper.GetType().Name);
try { ((IList<int>)wrapper).Add(5); } catch (Exception ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }
Console.WriteLine(((IList<int>)ro).IsReadOnly);   // ro 底层还是 List，强转回去能改

int[] arr = [1, 2, 3];
IList<int> ilist = arr;                        // 数组实现了 IList<T>
try { ilist.Add(4); } catch (Exception ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }
```
```
1
ro.Count=4
ReadOnlyCollection`1
NotSupportedException: Collection is read-only.
False
NotSupportedException: Collection was of a fixed size.
```

- 把 `List<T>` 声明成 `IReadOnlyList<T>` 返回，只是**编译期**不让改，调用方强转回 `IList<T>` 仍然能改。
- `AsReadOnly()` 返回 `ReadOnlyCollection<T>`，运行时也不能改，抛 `NotSupportedException`（Java `unmodifiableList` 抛 `UnsupportedOperationException`，一个意思）。
- 数组当 `IList<T>` 用时调 `Add` 也抛 `NotSupportedException`，和 Java `Arrays.asList(...).add()` 一样的坑。

### 3.3 ⚠️ foreach 时修改集合

```csharp
try
{
    foreach (var n in list)
        if (n == 2) list.Remove(n);
}
catch (Exception ex) { Console.WriteLine($"{ex.GetType().Name}: {ex.Message}"); }
```
```
InvalidOperationException: Collection was modified; enumeration operation may not execute.
```

相当于 Java 的 `ConcurrentModificationException`。Dictionary 遍历时加新 key 也会抛；但**修改已有 key 的值**（`d[k] = 100`，k 已存在）实测不会抛。

正确做法（三种写法结果都是 `1,3`）：

```csharp
list.RemoveAll(n => n % 2 == 0);                                   // 1. 首选
for (int i = list.Count - 1; i >= 0; i--)                          // 2. 倒序 for
    if (list[i] % 2 == 0) list.RemoveAt(i);
foreach (var n in list.ToList()) if (n % 2 == 0) list.Remove(n);   // 3. 遍历副本
```

## 4. LINQ 基础 🟢

LINQ（Language Integrated Query）= 一组 `IEnumerable<T>` 的扩展方法，在 `System.Linq` 命名空间（已隐式 using）。作用和 Java 8 Stream 一样：过滤、映射、排序、分组、聚合。

下面的例子都用这组数据：

```csharp
List<Emp> emps =
[
    new("Tom", "IT", 8000, 30),
    new("Amy", "HR", 6000, 25),
    new("Bob", "IT", 9000, 28),
    new("Zoe", "Sales", 7000, 35),
    new("Joe", "IT", 8000, 22),
];
List<Dept> depts = [new("IT", "研发部"), new("HR", "人事部"), new("Sales", "销售部")];

record Emp(string Name, string Dept, int Salary, int Age);
record Dept(string Code, string Name);
```

（`record` 见[第 9 章](09-modern-syntax.md)，这里当成一个带构造函数和属性的简单类就行。）

### 4.1 方法语法与查询语法

```csharp
// 方法语法（常用）
var q1 = emps.Where(e => e.Salary >= 8000)
             .OrderByDescending(e => e.Salary)
             .ThenBy(e => e.Name)
             .Select(e => e.Name);

// 查询语法（像 SQL，编译后和上面一样）
var q2 = from e in emps
         where e.Salary >= 8000
         orderby e.Salary descending, e.Name
         select e.Name;
Console.WriteLine(string.Join(",", q1));
Console.WriteLine(string.Join(",", q2));
```
```
Bob,Joe,Tom
Bob,Joe,Tom
```

日常用**方法语法**为主。查询语法在多表 `join`、需要中间变量（`let`）时更好读。

⚠️ 多字段排序：第二个字段用 `ThenBy`，**不要**再写一次 `OrderBy`，否则前面的排序被覆盖。

### 4.2 Select 与 SelectMany

`Select` 是一对一映射（Java `map`），`SelectMany` 是一对多再拍平（Java `flatMap`）。练习 06：

```csharp
int[][] selected = array2.Select(item => new[] { item, item * 10 }).ToArray();
int[] selectedMany = array2.SelectMany(item => new[] { item, item * 10 }).ToArray();
```
```
Select（数组的数组）:
  [1, 10]
  [2, 20]
  [3, 30]
  [4, 40]
  [5, 50]
SelectMany（拍平）: 1, 10, 2, 20, 3, 30, 4, 40, 5, 50
```

```csharp
string[] lines = ["a b", "c d e"];
Console.WriteLine(string.Join("|", lines.SelectMany(l => l.Split(' '))));
```
```
a|b|c|d|e
```

### 4.3 GroupBy 与 Join

```csharp
foreach (var g in emps.GroupBy(e => e.Dept))
    Console.WriteLine($"{g.Key}: count={g.Count()} avg={g.Average(e => e.Salary)} names={string.Join("/", g.Select(e => e.Name))}");
```
```
IT: count=3 avg=8333.333333333334 names=Tom/Bob/Joe
HR: count=1 avg=6000 names=Amy
Sales: count=1 avg=7000 names=Zoe
```

每个分组 `g` 是 `IGrouping<TKey, T>`：有 `Key`，本身又是一个序列，可以继续用 LINQ。Java 里要 `Collectors.groupingBy` 得到 `Map<K, List<T>>`，C# 直接遍历分组。

`Join` 是内连接：

```csharp
var joined = emps.Join(depts, e => e.Dept, d => d.Code, (e, d) => $"{e.Name}-{d.Name}");
Console.WriteLine(string.Join(",", joined));

var joined2 = from e in emps
              join d in depts on e.Dept equals d.Code
              where e.Age < 26
              select new { e.Name, DeptName = d.Name };   // 匿名类型
foreach (var x in joined2) Console.WriteLine(x);
```
```
Tom-研发部,Amy-人事部,Bob-研发部,Zoe-销售部,Joe-研发部
{ Name = Amy, DeptName = 人事部 }
{ Name = Joe, DeptName = 研发部 }
```

`Join` 的方法语法参数多，不好读；连表时查询语法更清楚。

### 4.4 聚合与判断

```csharp
Console.WriteLine($"Any>8500={emps.Any(e => e.Salary > 8500)} All>5000={emps.All(e => e.Salary > 5000)} Count(IT)={emps.Count(e => e.Dept == "IT")} Sum={emps.Sum(e => e.Salary)} Max={emps.Max(e => e.Salary)}");
Console.WriteLine(emps.MaxBy(e => e.Age));    // 返回整个对象，不只是最大值
```
```
Any>8500=True All>5000=True Count(IT)=3 Sum=38000 Max=9000
Emp { Name = Zoe, Dept = Sales, Salary = 7000, Age = 35 }
```

判断"有没有"用 `Any()`，不要写 `Count() > 0`：`Any` 找到一个就停，`Count` 要数完全部。

### 4.5 ⚠️ First / FirstOrDefault / Single

```csharp
Console.WriteLine(emps.First(e => e.Dept == "IT").Name);
Console.WriteLine(emps.FirstOrDefault(e => e.Dept == "Ops")?.Name ?? "null");
try { emps.First(e => e.Dept == "Ops"); }
catch (Exception ex) { Console.WriteLine($"First: {ex.GetType().Name}: {ex.Message}"); }
try { emps.Single(e => e.Dept == "IT"); }
catch (Exception ex) { Console.WriteLine($"Single: {ex.GetType().Name}: {ex.Message}"); }
Console.WriteLine(emps.Single(e => e.Dept == "HR").Name);
Console.WriteLine(emps.SingleOrDefault(e => e.Dept == "Ops") is null);
int[] empty = [];
Console.WriteLine(empty.FirstOrDefault());
try { empty.Max(); } catch (Exception ex) { Console.WriteLine($"Max: {ex.GetType().Name}: {ex.Message}"); }
```
```
Tom
null
First: InvalidOperationException: Sequence contains no matching element
Single: InvalidOperationException: Sequence contains more than one matching element
Amy
True
0
Max: InvalidOperationException: Sequence contains no elements.
```

| 方法 | 0 个 | 1 个 | 多个 |
|---|---|---|---|
| `First` | 抛异常 | 返回它 | 返回第一个 |
| `FirstOrDefault` | 返回 `default`（引用类型 `null`，`int` 是 0） | 返回它 | 返回第一个 |
| `Single` | 抛异常 | 返回它 | **抛异常** |
| `SingleOrDefault` | 返回 `default` | 返回它 | **抛异常** |

- 可能找不到 → `FirstOrDefault`，然后判 `null`。
- 按主键查、业务上必须唯一 → `Single`，多了说明数据有问题，让它报错。
- ⚠️ 值类型的 `FirstOrDefault` 找不到返回 0，和"找到了 0"分不清。可以用重载指定默认值：练习 06 里 `array2.LastOrDefault(n => n > 10, -1)` 输出 `-1`。
- ⚠️ 空序列的 `Max()` / `Min()` / `Average()` 抛异常（`Sum()` 返回 0）。

### 4.6 去重、分页、分块

```csharp
Console.WriteLine(string.Join(",", emps.Select(e => e.Salary).Distinct()));
Console.WriteLine(string.Join(",", emps.Select(e => e.Name).Skip(1).Take(2)));   // 分页：Skip((page-1)*size).Take(size)
foreach (var page in emps.Chunk(2)) Console.WriteLine($"[{string.Join(",", page.Select(e => e.Name))}]");
Console.WriteLine(string.Join(",", emps.DistinctBy(e => e.Dept).Select(e => e.Name)));  // 每个部门取第一个
```
```
8000,6000,9000,7000
Amy,Bob
[Tom,Amy]
[Bob,Zoe]
[Joe]
Tom,Amy,Zoe
```

`Chunk(n)` 把序列切成每块 n 个的数组，批量处理（比如每 100 条写一次库）时很好用。

### 4.7 ToDictionary / ToLookup

```csharp
var byName = emps.ToDictionary(e => e.Name, e => e.Salary);
Console.WriteLine(byName["Bob"]);
try { emps.ToDictionary(e => e.Dept); }
catch (Exception ex) { Console.WriteLine($"ToDictionary: {ex.GetType().Name}: {ex.Message}"); }
var lookup = emps.ToLookup(e => e.Dept);         // 一个 key 对应多个值
Console.WriteLine(string.Join(",", lookup["IT"].Select(e => e.Name)));
```
```
9000
ToDictionary: ArgumentException: An item with the same key has already been added. Key: IT
Tom,Bob,Joe
```

⚠️ `ToDictionary` 的 key 重复直接抛异常。key 可能重复时用 `ToLookup` 或 `GroupBy(...).ToDictionary(g => g.Key, g => g.ToList())`。

### 4.8 和 Java Stream 对照

| 操作 | C# LINQ | Java Stream |
|---|---|---|
| 过滤 | `Where` | `filter` |
| 映射 | `Select` | `map` |
| 一对多拍平 | `SelectMany` | `flatMap` |
| 排序 | `OrderBy` / `OrderByDescending`，次序 `ThenBy` | `sorted(Comparator.comparing(...).thenComparing(...))` |
| 分组 | `GroupBy` | `collect(groupingBy(...))` |
| 连接 | `Join` | 无，手写 |
| 是否存在 / 全部满足 | `Any` / `All` | `anyMatch` / `allMatch` |
| 第一个 | `First` / `FirstOrDefault` | `findFirst()` 返回 `Optional` |
| 去重 | `Distinct` / `DistinctBy` | `distinct` |
| 分页 | `Skip` / `Take` | `skip` / `limit` |
| 求和 / 计数 | `Sum` / `Count` | `mapToInt(...).sum()` / `count` |
| 转集合 | `ToList` / `ToArray` / `ToDictionary` / `ToHashSet` | `collect(toList())` 等 |
| 能否复用 | **可以多次枚举**（每次重新执行） | Stream 只能用一次 |

最大区别：Java Stream 用一次就关闭；LINQ 查询是一个"可以反复执行的查询定义"，每次 `foreach` 都重新算一遍。这就是下一节的内容。

## 5. 延迟执行与多次枚举 🟢 ⚠️

### 5.1 延迟执行：定义时不执行，枚举时才执行

```csharp
List<int> nums = [1, 2, 3];
var query = nums.Where(n =>
{
    Console.WriteLine($"  检查 {n}");
    return n > 1;
});
Console.WriteLine("1. 定义完 query，还没执行");
nums.Add(4);
Console.WriteLine("2. 第一次 foreach：");
foreach (var n in query) Console.WriteLine($"  得到 {n}");
Console.WriteLine($"3. Count() = {query.Count()}");
Console.WriteLine("4. ToList() 之后：");
var list = query.ToList();
nums.Add(5);
Console.WriteLine($"  list.Count={list.Count}, query.Count()={query.Count()}");
```
```
1. 定义完 query，还没执行
2. 第一次 foreach：
  检查 1
  检查 2
  得到 2
  检查 3
  得到 3
  检查 4
  得到 4
  检查 1
  检查 2
  检查 3
  检查 4
3. Count() = 3
4. ToList() 之后：
  检查 1
  检查 2
  检查 3
  检查 4
  检查 1
  检查 2
  检查 3
  检查 4
  检查 5
  list.Count=3, query.Count()=4
```

从输出能看出：

1. 定义 `query` 时一行"检查"都没打印。
2. 后加的 `4` 也被查到了：查询在 `foreach` 时才去读 `nums`。
3. 元素是**一个一个流过去**的（检查 2 → 得到 2 → 检查 3 → 得到 3），不是先全部过滤完再输出。
4. `Count()` 又把整个 `Where` 跑了一遍。
5. `ToList()` 立刻执行并把结果存下来；之后 `nums` 再变，`list` 不变，但 `query` 每次都是最新结果。

| 延迟执行（返回 `IEnumerable<T>`） | 立即执行 |
|---|---|
| `Where`、`Select`、`SelectMany`、`OrderBy`、`GroupBy`、`Join`、`Distinct`、`Skip`、`Take`、`Chunk` | `ToList`、`ToArray`、`ToDictionary`、`Count`、`Sum`、`Max`、`Any`、`All`、`First`、`Single`、`foreach` |

### 5.2 多次枚举的代价

```csharp
int calls = 0;
IEnumerable<int> expensive = Enumerable.Range(1, 3).Select(n => { calls++; return n * n; });
if (expensive.Any()) Console.WriteLine($"Sum={expensive.Sum()} Max={expensive.Max()}");
Console.WriteLine($"Select 里的代码执行了 {calls} 次");
```
```
Sum=14 Max=9
Select 里的代码执行了 7 次
```

`Any` 1 次 + `Sum` 3 次 + `Max` 3 次 = 7 次。如果 `Select` 里是查数据库、调接口，就是 7 次远程调用。IDE 会对这种写法给出 "Possible multiple enumeration" 提示。

### 5.3 闭包与异常也是延迟的

```csharp
int min = 1;
var q2 = nums.Where(n => n > min);
min = 3;                                         // 定义之后才改
Console.WriteLine(string.Join(",", q2));         // 用的是枚举时的 min

var bad = nums.Select(n => 10 / (n - 3));
Console.WriteLine("bad 定义完，没报错");
try { foreach (var x in bad) Console.Write($"{x} "); }
catch (DivideByZeroException) { Console.WriteLine("\n枚举到 3 时才抛 DivideByZeroException"); }
```
```
4,5
bad 定义完，没报错
-5 -10 
枚举到 3 时才抛 DivideByZeroException
```

异常不在写查询的那一行抛，而是在用结果的地方抛，看堆栈时要注意。

### 5.4 什么时候 ToList

- 结果要**用两次以上**（`Any` 之后再 `foreach`、`Count` 之后再遍历）→ 先 `ToList()`。
- 要**脱离数据源**（之后源集合会被修改；或者源是数据库连接，马上要关闭）→ `ToList()`。
- 方法要返回结果给别人 → 一般 `ToList()` 后以 `IReadOnlyList<T>` 返回，避免调用方拿到一个每次都重算、还可能抛异常的查询。
- 只遍历一次、数据量很大 → 不要 `ToList()`，让它流式处理，省内存。

## 6. yield return 迭代器

自己写一个返回 `IEnumerable<T>` 的方法，用 `yield return` 一个一个"吐"元素，也是延迟执行的。Java 里要手写 `Iterator` 类（`hasNext`/`next`），C# 编译器帮你生成。

```csharp
foreach (var n in Evens(10).Take(3)) Console.WriteLine($"拿到 {n}");
Console.WriteLine("---");
var it = Evens(10);
Console.WriteLine("调用 Evens 后还没输出任何东西");
foreach (var n in Fib().TakeWhile(x => x < 30)) Console.Write($"{n} ");
Console.WriteLine();

static IEnumerable<int> Evens(int max)
{
    Console.WriteLine("  Evens 开始执行");
    for (int i = 0; i <= max; i += 2)
    {
        Console.WriteLine($"  yield {i}");
        yield return i;
    }
    Console.WriteLine("  Evens 结束");
}

static IEnumerable<long> Fib()      // 无限序列，靠调用方 Take/TakeWhile 截断
{
    long a = 0, b = 1;
    while (true) { yield return a; (a, b) = (b, a + b); }
}
```
```
  Evens 开始执行
  yield 0
拿到 0
  yield 2
拿到 2
  yield 4
拿到 4
---
调用 Evens 后还没输出任何东西
0 1 1 2 3 5 8 13 21 
```

- 调用 `Evens(10)` 时方法体一行都没执行，第一次 `MoveNext`（foreach 取第一个）才开始。
- `Take(3)` 拿够 3 个就不再要了，所以 "Evens 结束" 没有打印，6、8、10 也没算。
- `yield break;` 提前结束序列。

⚠️ 参数校验也会被延迟：

```csharp
try { var r = Check(null); Console.WriteLine("Check(null) 没抛"); r.ToList(); }
catch (ArgumentNullException) { Console.WriteLine("ToList 时才抛 ArgumentNullException"); }

static IEnumerable<char> Check(string? s)
{
    ArgumentNullException.ThrowIfNull(s);
    foreach (var c in s) yield return c;
}
```
```
Check(null) 没抛
ToList 时才抛 ArgumentNullException
```

需要立即校验时，拆成两个方法：外层普通方法做校验，再调用里面用 `yield` 的本地函数。

## 7. 小结

1. 日常用 `List<T>` 和 `Dictionary<K,V>`；数组用 `Length`，集合用 `Count`。
2. `dict[key]` 取不存在的 key 抛 `KeyNotFoundException`，取值用 `TryGetValue` 或 `GetValueOrDefault`。
3. 参数用 `IEnumerable<T>`，返回值用 `IReadOnlyList<T>`；C# 的 `List<T>` 是类，`IList<T>` 才是接口。
4. `foreach` 里不能增删集合元素，用 `RemoveAll`、倒序 `for` 或遍历副本。
5. LINQ ≈ Java Stream：`Where`/`Select`/`SelectMany`/`OrderBy`+`ThenBy`/`GroupBy`；判断存在用 `Any`。
6. 可能找不到用 `FirstOrDefault`，必须唯一用 `Single`；空序列 `Max` 会抛异常；`ToDictionary` key 重复会抛异常。
7. LINQ 是延迟执行的，每枚举一次就重算一次；结果要多次使用或要脱离数据源时先 `ToList()`。
8. `yield return` 写迭代器，同样是延迟执行，参数校验也会被推迟。

---

上一章：[委托、Lambda 与事件 🟢](06-delegates-events.md) · [目录](../README.md) · 下一章：[异常与资源释放 🟢](08-exceptions-resources.md)
