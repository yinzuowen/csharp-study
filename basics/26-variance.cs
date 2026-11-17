// 主题：泛型的协变（out T）与逆变（in T）—— 只能用在接口和委托上
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// 普通赋值：string → object 天然可以
string name = "Owen";
object obj = name;

// ---------- 协变 out T：T 只出现在返回值，赋值方向和 T 一致 ----------
IProducer<string> strProducer = new Producer<string>(name);
IProducer<object> objProducer = strProducer;          // ✅ IProducer<string> → IProducer<object>
// strProducer = objProducer;                         // ❌ 反方向不行
Console.WriteLine($"协变：{objProducer.Get()}");

// 内置例子：IEnumerable<out T>
IEnumerable<object> objects = new List<string> { "a", "b" };
Console.WriteLine($"IEnumerable<object> = List<string>: {string.Join(", ", objects)}");

// ---------- 逆变 in T：T 只出现在参数，赋值方向和 T 相反 ----------
IConsumer<object> objConsumer = new Consumer<object>();
IConsumer<string> strConsumer = objConsumer;          // ✅ IConsumer<object> → IConsumer<string>
// objConsumer = strConsumer;                         // ❌ 否则可以 objConsumer.Handle(123)，交给只会处理 string 的对象
strConsumer.Handle("能处理 object 的，当然也能处理 string");

// 内置例子：Action<in T>
Action<object> printAny = o => Console.WriteLine($"Action 逆变：{o}");
Action<string> printStr = printAny;
printStr("Owen");

// Func<in T, out TResult>：参数逆变、返回值协变
Func<object, string> f1 = o => $"[{o}]";
Func<string, object> f2 = f1;
Console.WriteLine($"Func：{f2("x")}");

// 注意：只对引用类型有效。IProducer<int> 不能赋给 IProducer<object>（需要装箱）

interface IProducer<out T>
{
    T Get();                 // ✅ 返回值
    // void Set(T item);     // ❌ 协变的 T 不能当参数
}

class Producer<T> : IProducer<T>
{
    private readonly T _value;
    public Producer(T value) => _value = value;
    public T Get() => _value;
}

interface IConsumer<in T>
{
    void Handle(T item);     // ✅ 参数
    // T Get();              // ❌ 逆变的 T 不能当返回值
}

// 类本身不能写 in / out，只是把 T 原样传给接口
class Consumer<T> : IConsumer<T>
{
    public void Handle(T item) => Console.WriteLine($"逆变：{item}");
}
