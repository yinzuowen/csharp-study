#:property PublishAot=false
// 主题：反射 —— 读写字段（包括 private），FieldType vs GetType()
using System.Reflection;

namespace HelloWorld
{
    internal class Person
    {
        public string name = string.Empty;

    #pragma warning disable CS0414            // 编译器不知道会通过反射读取它
        private int age = 0;
    #pragma warning restore CS0414

        public override string ToString() => $"Person({name}, {age})";
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Type type = typeof(Person);

            // 默认只能拿到 public；要 private 必须同时写 NonPublic 和 Instance
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo field in type.GetFields(flags))
            {
                // FieldType：字段声明的类型；field.GetType()：反射对象自己的类型（RtFieldInfo）
                Console.WriteLine($"{field.Name}: FieldType={field.FieldType.Name}, GetType()={field.GetType().Name}");
            }

            var person = new Person();
            FieldInfo? name = type.GetField("name");
            FieldInfo? age = type.GetField("age", BindingFlags.NonPublic | BindingFlags.Instance);

            name?.SetValue(person, "Owen");
            age?.SetValue(person, 36);                  // 连 private 字段都能改
            Console.WriteLine($"{name?.GetValue(person)}, {age?.GetValue(person)}");
            Console.WriteLine(person);
        }
    }
}
