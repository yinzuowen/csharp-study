#:property PublishAot=false
// 主题：反射 —— GetMembers 与 MemberInfo 家族
//
// MemberInfo（抽象基类）
// ├── FieldInfo / PropertyInfo / EventInfo
// ├── MethodBase → MethodInfo / ConstructorInfo
// └── Type
using System.Reflection;

// 默认只返回 public 成员，包括从 object 继承来的

namespace HelloWorld
{
    internal class Person
    {
        private int _age = 18;
        public event Action<Person>? OnCompleted;
        public string Name { get; set; } = string.Empty;

        public void Show()
        {
            Console.WriteLine($"{Name}, {_age}");
            OnCompleted?.Invoke(this);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            foreach (MemberInfo member in typeof(Person).GetMembers())
            {
                Console.WriteLine($"{member.MemberType,-12} {member.Name}");
            }
            // 可以看到：属性 Name 编译后是 get_Name / set_Name 两个方法，事件是 add_ / remove_ 两个方法

            Console.WriteLine("--------------------------");

            // BindingFlags：传了就必须同时指定 Instance 或 Static，否则什么都查不到
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (MemberInfo member in typeof(Person).GetMembers(flags))
            {
                // 按种类区分；IsSpecialName 可以排除 get_/set_/add_/remove_ 这些编译器生成的方法
                string text = member switch
                {
                    PropertyInfo p => $"属性 {p.Name}: {p.PropertyType.Name}",
                    FieldInfo f => $"字段 {f.Name}: {f.FieldType.Name}",
                    EventInfo e => $"事件 {e.Name}: {e.EventHandlerType?.Name}",
                    ConstructorInfo c => $"构造 {c}",
                    MethodInfo m when !m.IsSpecialName => $"方法 {m.Name}: {m.ReturnType.Name}",
                    _ => ""
                };
                if (text != "") Console.WriteLine(text);
            }
        }
    }
}
