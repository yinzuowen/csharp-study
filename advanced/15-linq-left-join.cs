// 主题：左外连接 —— join ... into + from ... in xxx.DefaultIfEmpty()，没匹配到的一侧为 null

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
                new Student("张三", "0003"),
                new Student("李四", "0001"),
                new Student("王五", "0003"),
                new Student("赵六", "0008")
            };

            var collection = from student in students
                             join subject in subjects on student.SubjectId equals subject.Id into array
                             from temp in array.DefaultIfEmpty()
                             select new { student.Name, Course = temp?.Course ?? "（无）" };   // temp 可能为 null，等价于 temp != null ? temp.Course : "（无）"

            foreach (var item in collection)
            {
                Console.WriteLine($"{item.Name}: {item.Course}");          // 左连接后每行只有一门课，不需要 string.Join
            }
        }
    }
}