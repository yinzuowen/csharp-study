// 主题：泛型 —— 泛型结构体、泛型委托、泛型方法、类型参数约束（where）
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

// ---------- 泛型结构体 ----------
var point = new Point<int>(100, 200);
Console.WriteLine(point);
var pointD = new Point<double>(1.5, 2.5);
Console.WriteLine(pointD);

Console.WriteLine("--------------------------");

// ---------- 泛型委托：同一个委托定义，可以用在不同的类型上 ----------
DeleteHandler<UserEntity> handler1 = OnDelete;            // 1. 用现成的方法
handler1(new UserEntity { Id = 1 });

DeleteHandler<UserEntity> handler2 = entity =>            // 2. 用 lambda（最常用）
{
    Console.WriteLine($"lambda 删除 {entity.Id}");
    return true;
};
handler2(new UserEntity { Id = 2 });

var handler3 = new DeleteHandler<UserEntity>(OnDelete);   // 3. 完整的 new 写法，用圆括号不是 { }
handler3(new UserEntity { Id = 3 });

// DeleteHandler<T> 和内置的 Func<T, bool>、Predicate<T> 签名完全一样
Func<UserEntity, bool> sameAsFunc = OnDelete;

Console.WriteLine("--------------------------");

// ---------- 泛型方法：调用时通常不用写 <T>，编译器会根据参数推断 ----------
int a = 1, b = 2;
Swap(ref a, ref b);              // 等价于 Swap<int>(ref a, ref b)
Console.WriteLine($"a={a}, b={b}");

string x = "x", y = "y";
Swap(ref x, ref y);
Console.WriteLine($"x={x}, y={y}");

Console.WriteLine("--------------------------");

// ---------- 类型参数约束 ----------
var box = new Box<int>();
// var box2 = new Box<string>();  // ❌ CS0453：where T : struct 要求值类型，string 是引用类型

var repo = new Repo<UserEntity>();
repo.Create(10);
repo.Create(20);
Console.WriteLine($"Find(20): {repo.Find(20)?.Id}");
Console.WriteLine($"Find(99): {repo.Find(99)?.Id ?? -1}");
// var repo2 = new Repo<int>();   // ❌ int 不是 class，也没实现 IEntity

static bool OnDelete(UserEntity entity)
{
    Console.WriteLine($"删除 {entity.Id}");
    return true;
}

static void Swap<T>(ref T x, ref T y)
{
    (x, y) = (y, x);
}

struct Point<T>
{
    public T X;
    public T Y;

    public Point(T x, T y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}

delegate bool DeleteHandler<T>(T entity);

interface IEntity
{
    int Id { get; set; }
}

class UserEntity : IEntity
{
    public int Id { get; set; }
}

// 常用约束：
// where T : struct          值类型（不可空）
// where T : class           引用类型
// where T : class?          引用类型，可以为空
// where T : notnull         不可空（值类型、引用类型都可以）
// where T : new()           必须有 public 无参构造函数（可以 new T()），必须放在最后
// where T : Person          必须是 Person 或它的子类
// where T : IComparable<T>  必须实现该接口
// where T : unmanaged       不含引用的值类型（可用于指针）
class Box<T> where T : struct
{
}

// 多个约束组合：主约束（class/struct/基类）放第一个，接口在中间，new() 放最后
class Repo<T> where T : class, IEntity, new()
{
    private readonly List<T> _items = [];

    public T Create(int id)
    {
        var item = new T();     // 靠 new() 约束
        item.Id = id;           // 靠 IEntity 约束
        _items.Add(item);
        return item;
    }

    public T? Find(int id) => _items.FirstOrDefault(x => x.Id == id);   // 找不到返回 null，靠 class 约束
}
