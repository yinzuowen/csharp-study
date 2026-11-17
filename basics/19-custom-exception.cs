// 主题：自定义异常（泛型异常类、: base(message)、throw / catch）
//
// 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
// 引用类型：class（含 object/string、record）、interface、delegate、数组
// 指针类型：int* 等，只能在 unsafe 代码中使用

var person = new Person(-18);

try
{
    Check(person);
}
catch (DataEntityException<Person> ex)      // 只捕获这一种异常，其他异常继续往外抛
{
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"Entity.Age: {ex.Entity.Age}");   // 异常里还能带上出问题的对象
}
finally
{
    Console.WriteLine("finally：无论是否异常都会执行");
}

static void Check(Person person)
{
    if (person.Age < 0)
    {
        throw new DataEntityException<Person>(person, "age must be greater than 0");
    }
}

class Person
{
    public int Age { get; init; }

    public Person(int age)
    {
        Age = age;
    }
}

// 自定义异常：继承 Exception，类名按惯例以 Exception 结尾
class DataEntityException<T> : Exception
{
    public T Entity { get; }

    // : base(message) 把消息交给父类 Exception，这样 ex.Message 才能拿到它
    // （相当于 Java 的 super(message)）
    public DataEntityException(T entity, string message) : base(message)
    {
        Entity = entity;
    }
}
