// 主题：private set、init、required、[SetsRequiredMembers]
using System.Diagnostics.CodeAnalysis;

// ---------- required：创建对象时必须在 { } 里赋值 ----------
var p1 = new Person { Age = 20 };
// var p0 = new Person();                     // ❌ 编译错误：必须给 required 的 Age 赋值
Console.WriteLine($"p1: Age={p1.Age}, Birthday={p1.Birthday:yyyy-MM-dd}");

// ---------- init：只能在创建时赋值，之后只读 ----------
// p1.Age = 30;                               // ❌ 编译错误：init 属性创建后不能改

// ---------- private set：外部只读，只能通过类自己的方法修改 ----------
// p1.Birthday = DateTime.Today;              // ❌ 外部不能赋值
p1.SetBirthday(DateTime.Parse("2006-01-01"));
Console.WriteLine($"p1: Birthday={p1.Birthday:yyyy-MM-dd}");

// ---------- [SetsRequiredMembers]：构造函数承诺"会给所有 required 成员赋值" ----------
var p2 = new Person(DateTime.Parse("1988-12-28"));   // 不用再写 { Age = ... }
Console.WriteLine($"p2: Age={p2.Age}, Birthday={p2.Birthday:yyyy-MM-dd}");

var p3 = new Person(18);
Console.WriteLine($"p3: Age={p3.Age}");

class Person
{
    public DateTime Birthday { get; private set; }
    public required int Age { get; init; }

    public Person() { }

    // 编译器不会检查构造函数里有没有给 Age 赋值，
    // 标上这个特性，相当于告诉编译器"我保证赋值了"（编译器不会验证这个承诺）
    [SetsRequiredMembers]
    public Person(DateTime birthday)
    {
        Birthday = birthday;
        Age = DateTime.Today.Year - birthday.Year;   // 简化：只按年份差算，精确算法见 15-extension-methods.cs
    }

    [SetsRequiredMembers]
    public Person(int age)
    {
        Age = age;
    }

    public void SetBirthday(DateTime birthday)
    {
        Birthday = birthday;
    }
}
