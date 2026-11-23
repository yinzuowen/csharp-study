// 主题：select 投影成匿名类型 —— new { ... } 只取需要的字段，还可以计算新字段

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
            students.Add(new Student("赵六", new List<int> { 88, 60, 45 }));

            var collection = from student in students
                             select new
                             {
                                 student.Name,
                                 English = student.Scores[2],
                                 Level = student.Scores[2] >= 60 ? "及格" : "不及格"
                             };

            foreach (var item in collection)
            {
                Console.WriteLine($"{item}");
            }
        }
    }
}