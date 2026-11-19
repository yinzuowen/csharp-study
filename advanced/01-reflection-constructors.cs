#:property PublishAot=false
// 主题：反射 —— 获取构造函数、通过构造函数 / Activator 创建对象
using System.Reflection;

namespace HelloWorld
{
    internal class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public Person(string name) => Name = name;

        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Type type = typeof(Person);

            // ---------- GetConstructors()：所有 public 构造函数（复数，返回数组） ----------
            foreach (ConstructorInfo ctor in type.GetConstructors())
            {
                // 构造函数的 Name 永远是 .ctor；直接打印 ctor 可以看到参数
                Console.WriteLine($"{ctor.Name}: {ctor}");
            }

            // ---------- GetConstructor(Type[])：按参数类型找某一个（单数，可能返回 null） ----------
            ConstructorInfo? constructor = type.GetConstructor([typeof(string), typeof(int)]);

            // ConstructorInfo.Invoke 没有 params，参数必须放进数组
            if (constructor?.Invoke(["Owen", 36]) is Person person)
            {
                Console.WriteLine($"ConstructorInfo.Invoke: {person.Name}, {person.Age}");
            }

            // ---------- Activator.CreateInstance：更简单，内部也是找构造函数 ----------
            // 签名带 params，所以逐个传、传数组都可以
            object? p1 = Activator.CreateInstance(type, "Zoe", 10);
            object? p2 = Activator.CreateInstance(type, new object[] { "Zoe", 10 });
            Console.WriteLine($"Activator: {(p1 as Person)?.Name}, {(p2 as Person)?.Name}");

            // 注意：GetType() 返回的是反射对象自己的类型，不是构造函数的信息
            Console.WriteLine($"constructor.GetType().Name = {constructor?.GetType().Name}");
        }
    }
}
