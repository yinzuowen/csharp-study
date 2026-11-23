// 主题：join ... on ... equals —— 内连接，两边都匹配上才保留（赵六没有对应科目，不出现）

namespace HelloWorld
{
    internal class Student
    {
        public string Name { get; set; }
        public string SubjectId { get; set; }

        public Student(string name, string subjectId)
        {
            Name = name;
            SubjectId = subjectId;
        }
    }

    internal class Subject
    {
        public string Id { get; set; }
        public string Course { get; set; }

        public Subject(string id, string course)
        {
            this.Id = id;
            this.Course = course;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            List<Subject> subjects = new List<Subject>()
            {
                new Subject("0001", "声乐"),
                new Subject("0002", "美术"),
                new Subject("0003", "篮球")
            };

            List<Student> students = new List<Student>()
            {
                new Student("张三", "0002"),
                new Student("李四", "0001"),
                new Student("王五", "0003"),
                new Student("赵六", "0008")
            };

            var collection = from student in students
                             join subject in subjects on student.SubjectId equals subject.Id
                             select new { student.Name, subject.Course };

            foreach (var item in collection)
            {
                Console.WriteLine($"{item.Name}: {item.Course}");
            }
        }
    }
}