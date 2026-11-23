// 主题：group by —— 结果是 IGrouping<TKey, T> 的序列，每组有 Key，本身又是一个集合

namespace HelloWorld
{
    internal class Person
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }

        public Person(string name, string gender, int age)
        {
            Name = name;
            Gender = gender;
            Age = age;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            List<Person> persons = new List<Person>();

            persons.Add(new Person("张三", "男", 18));
            persons.Add(new Person("李四", "女", 26));
            persons.Add(new Person("王五", "男", 48));
            persons.Add(new Person("赵六", "女", 45));

            Console.WriteLine("group by gender:");

            var groupByGender = from person in persons
                                group person by person.Gender;

            foreach (var item in groupByGender)
            {
                // item 本身就是这一组的 Person 集合
                Console.WriteLine($"{item.Key}: {string.Join(", ", item.Select(p => p.Name))}");
            }

            Console.WriteLine("group by age:");

            var groupByAge = from person in persons
                             group person by person.Age / 10;

            foreach (var item in groupByAge)
            {
                Console.WriteLine($"{item.Key * 10}-{item.Key * 10 + 9}: {string.Join(", ", item.Select(p => p.Name))}");
            }
        }
    }
}