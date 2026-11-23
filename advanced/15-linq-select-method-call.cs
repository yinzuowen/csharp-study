// 主题：select 里调用自定义方法 —— 查询表达式里可以调用任意方法

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
                                 Level = GetLevel(student)
                             };

            foreach (var item in collection)
            {
                Console.WriteLine($"{item}");
            }
        }

        static string GetLevel(Student student)
        {
            // 不要写 Sum() / Count：int / int 是整数除法，小数部分会被丢掉
            double avg = student.Scores.Average();

            if (avg >= 90)
            {
                return "A";
            }

            if (avg >= 80)
            {
                return "B";
            }

            if (avg >= 70)
            {
                return "C";
            }

            if (avg >= 60)
            {
                return "D";
            }

            return "E";
        }
    }
}