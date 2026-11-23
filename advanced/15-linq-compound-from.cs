// 主题：复合 from —— 两个 from 把每个学生的成绩列表展开成一行一行（相当于 SelectMany）

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
                             from score in student.Scores
                             where score >= 60
                             select (student.Name, score);

            foreach (var item in collection)
            {
                Console.WriteLine($"{item.Name}: {item.score}");
            }
        }
    }
}