// 主题：多个 where 条件 —— 用 && 合并，或者写多个 where 子句，效果相同

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

            // var collection = from student in students
            //                  where student.Scores[0] >= 60
            //                  where student.Scores[1] >= 80
            //                  select student.Name;

            var collection = from student in students
                             where student.Scores[0] >= 60 && student.Scores[1] >= 80
                             select student.Name;

            foreach (var item in collection)
            {
                Console.WriteLine($"{item}");
            }
        }
    }
}