// 主题：Distinct —— 自定义类要重写 Equals 和 GetHashCode 才能按内容去重，否则比较的是引用

namespace HelloWorld
{
    internal class Student
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public Student(string name, int age)
        {
            Name = name;
            Age = age;
        }

        // Distinct 先比较 GetHashCode，相等时再调用 Equals，所以两个都要重写
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            // obj 为 null 或者不是 Student 时返回 false；直接写 (Student)obj 遇到其他类型会抛 InvalidCastException
            return obj is Student other && Name == other.Name && Age == other.Age;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Age);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            List<Student> students = new List<Student>();

            students.Add(new Student("张三", 18));
            students.Add(new Student("李四", 20));
            students.Add(new Student("张三", 18));

            var collection = students.Distinct().ToList();

            foreach (var item in collection)
            {
                Console.WriteLine($"{item.Name}: {item.Age}");
            }
        }
    }
}