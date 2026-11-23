// 主题：join ... into —— 分组连接（GroupJoin），每个学生一行，匹配到的科目放进一个集合，没匹配到就是空集合

namespace HelloWorld
{
    internal class Student(string name, string subjectId)
    {
        public string Name { get; set; } = name;
        public string SubjectId { get; set; } = subjectId;
    }

    internal class Subject(string id, string course)
    {
        public string Id { get; set; } = id;
        public string Course { get; set; } = course;
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
                             join subject in subjects on student.SubjectId equals subject.Id into array
                             select new { student.Name, Courses = array.Select(s => s.Course).ToList() };

            foreach (var item in collection)
            {
                Console.WriteLine($"{item.Name}: [{string.Join(", ", item.Courses)}]");
            }
        }
    }
}