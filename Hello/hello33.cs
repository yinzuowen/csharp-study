#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 泛型委托
    internal delegate bool DeleteHandler<T>(T entity);

    internal class UserEntity { }

    internal class Program
    {
        static void Main(string[] args)
        {
            // 1. 用现成的方法
            DeleteHandler<UserEntity> handler1 = OnDelete;
            handler1.Invoke(new UserEntity());

            // 2. 用 lambda（最常用）
            DeleteHandler<UserEntity> handler2 = entity =>
            {
                Console.WriteLine($"删除 {entity}");
                return true;
            };
            handler2.Invoke(new UserEntity());

            // 3. 完整的 new 写法（老写法，括号里放方法或 lambda）
            var handler3 = new DeleteHandler<UserEntity>(OnDelete);
            handler3.Invoke(new UserEntity());
        }

        static bool OnDelete(UserEntity entity)
        {
            Console.WriteLine($"删除 {entity}");
            return true;
        }
    }
}