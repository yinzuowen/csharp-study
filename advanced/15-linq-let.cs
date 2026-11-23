// 主题：let 子句 —— 在查询中定义中间变量，避免重复计算

namespace HelloWorld
{
    internal class Student
    {
        public string Name { get; set; }
        public List<int> Scores { get; }

        public Student(string name, List<int> scores)
        {
            Name = name;
            Scores = scores;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            List<Student> students = new List<Student>();

            students.Add(new Student("张三", new List<int> { 77, 88, 99 }));
            students.Add(new Student("李四", new List<int> { 55, 66, 66 }));
            students.Add(new Student("王五", new List<int> { 0, 60, 100 }));

            var collection = from student in students
                             let name = student.Name
                             let count = student.Scores.FindAll(s => s == 100).Count
                             let percent = 100f * count / student.Scores.Count
                             where count > 0
                             select new
                             {
                                Name = name,
                                Count = count,
                                Percent = percent
                             };

            foreach (var item in collection)
            {
                Console.WriteLine($"{item.Name}: {item.Count}-{item.Percent:F1}%");
            }
        }
    }
}