// 主题：扩展方法（给已有类型"添加"方法，不用修改原类、不用继承）

Person person = new Person { Birthday = DateTime.Parse("1988-12-28") };
Console.WriteLine($"生日：{person.Birthday:yyyy-MM-dd}");
Console.WriteLine($"年龄：{person.GetAge()}");            // 看起来像 Person 自己的方法
Console.WriteLine($"年龄：{PersonExtensions.GetAge(person)}");   // 编译器实际调用的是这个

// 系统类型也能扩展
Console.WriteLine($"\"\".IsEmpty() = {"".IsEmpty()}");
Console.WriteLine($"\"hi\".IsEmpty() = {"hi".IsEmpty()}");

// LINQ 的 Where / Select 等也都是扩展方法（定义在 System.Linq.Enumerable 里）
int[] nums = [1, 2, 3];
Console.WriteLine(string.Join(", ", nums.Where(x => x > 1)));

// 扩展方法必须定义在 非嵌套、非泛型 的 static class 里；
// 类名随意，习惯叫 "被扩展的类型 + Extensions"
public static class PersonExtensions
{
    // 第一个参数前加 this：表示这是 Person 的扩展方法
    public static int GetAge(this Person person)
    {
        // 不要用 天数/365 再向上取整：会多算 1 岁，而且闰年会有误差
        var today = DateTime.Today;
        int age = today.Year - person.Birthday.Year;
        if (today < person.Birthday.AddYears(age)) age--;   // 今年生日还没到
        return age;
    }
}

public static class StringExtensions
{
    public static bool IsEmpty(this string s) => string.IsNullOrEmpty(s);
}

// sealed：不能被继承，但仍然可以通过扩展方法添加功能
public sealed class Person
{
    public DateTime Birthday { get; set; }
}
