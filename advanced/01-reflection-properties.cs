#:property PublishAot=false
// 主题：反射 —— 获取属性、读写属性值
using System.Reflection;

namespace HelloWorld
{
    internal class Person
    {
        public string Name { get; set; } = string.Empty;
        public string Initial => Name.Length > 0 ? Name[..1] : "";   // 只读属性：CanWrite = False
        public int Age = 0;                                         // 字段
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Type type = typeof(Person);

            // GetProperties 只返回属性，不包括字段（Age 是字段，不会出现）
            foreach (PropertyInfo property in type.GetProperties())
            {
                Console.WriteLine($"{property.MemberType}: {property.Name} - {property.PropertyType.Name}, CanWrite={property.CanWrite}");
            }

            var person = new Person();
            PropertyInfo? nameProperty = type.GetProperty("Name");
            nameProperty?.SetValue(person, "Owen");
            Console.WriteLine($"Name = {nameProperty?.GetValue(person)}");
        }
    }
}
