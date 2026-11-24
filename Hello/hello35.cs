#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 泛型的类型参数约束
    // where T : struct -> 值类型（不能为空）
    // where T : class -> 引用类型
    // where T : class? -> 引用类型（可以为空）
    // where T : notnull -> 不可空（值类型、引用类型都可以）
    // where T : new() -> 必须有无参构造函数（可以 new T()）
    // where T : Person -> 必须是 Person 或它的子类
    // where T : IComparable<T> -> 必须实现该接口
    // where T : unmanaged -> 不含引用的值类型（可用于指针）

    internal class Box<T> where T : struct
    {

    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Box<int> box1 = new Box<int>();
            Box<string> box2 = new Box<string>();
        }
    }
}