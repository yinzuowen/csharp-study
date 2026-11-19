#:property PublishAot=false
// 主题：自定义特性（Attribute）—— 定义、使用、通过反射读取
using System.Reflection;

namespace HelloWorld
{
    // 特性就是继承 Attribute 的类，类名按惯例以 Attribute 结尾，使用时可以省略这个后缀
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]   // 只能用在类上，可以标记多次
    internal class AuthorAttribute : Attribute
    {
        public string Name { get; }
        public string Message { get; set; }

        // 参数默认值必须是编译时常量：string.Empty 是 static readonly，不能用，要写 ""
        public AuthorAttribute(string name, string message = "")
        {
            Name = name;
            Message = message;
        }
    }

    [Author("Owen", Message = "first version")]   // 构造函数参数 + 命名属性
    [Author("Zoe")]
    internal class Cat
    {
        public void Show() => Console.WriteLine("Meow");
    }

    internal class Dog
    {
        public void Show() => Console.WriteLine("Wang");
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Cat cat = CreateInstance<Cat>();
            cat.Show();

            var dog = CreateInstance<Dog>();          // Dog 没有标记特性，GetCustomAttribute 返回 null
            dog.Show();

            // 读取所有标记（AllowMultiple = true 时可以标记多次）
            foreach (var attr in typeof(Cat).GetCustomAttributes<AuthorAttribute>())
            {
                Console.WriteLine($"Author: {attr.Name}, {attr.Message}");
            }
        }

        // new() 约束：可以直接 new T()，不用 Activator 反射，也不会返回 null
        static T CreateInstance<T>() where T : new()
        {
            // GetCustomAttribute（单数）：找不到返回 null；但同一特性标记了多次时会抛 AmbiguousMatchException，
            // 所以 AllowMultiple = true 的特性要用 GetCustomAttributes（复数）
            var attribute = typeof(T).GetCustomAttributes<AuthorAttribute>().FirstOrDefault();
            Console.WriteLine(attribute != null ? $"[{typeof(T).Name}] 第一个 Author: {attribute.Name}" : $"[{typeof(T).Name}] 没有 Author");
            return new T();
        }
    }
}
