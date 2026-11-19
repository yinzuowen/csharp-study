#:property PublishAot=false
// 主题：反射 —— 获取方法、查看参数、调用方法（含触发事件）
using System.Reflection;

namespace HelloWorld
{
    internal class Calculator
    {
        public int Add(int x, int y) => x + y;
    }

    internal class Person
    {
        public event Action<Person>? OnCompleted;
        public string Name { get; }
        public int Age { get; }

        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public void Show()
        {
            Console.WriteLine($"name: {Name}, age: {Age}");
            OnCompleted?.Invoke(this);    // 只有类内部能触发事件
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Type type = typeof(Calculator);
            object? calculator = Activator.CreateInstance(type);

            MethodInfo method = type.GetMethod("Add") ?? throw new InvalidOperationException("Add not found");

            // ParameterType 才是参数的类型；parameter.GetType() 得到的是 RuntimeParameterInfo
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                Console.WriteLine($"{parameter.Name}: {parameter.ParameterType.Name}");
            }
            Console.WriteLine($"ReturnType: {method.ReturnType.Name}");

            // MethodInfo.Invoke(对象, 参数数组)：没有 params，必须传数组；无参方法传 null 或 []
            object? result = method.Invoke(calculator, [1, 2]);
            Console.WriteLine($"{method.Name}: 1 + 2 = {result}");

            // Invoke 返回 object?，需要转换后才能当 int 用
            if (result is int sum)
            {
                Console.WriteLine($"sum * 10 = {sum * 10}");
            }

            Console.WriteLine("--------------------------");

            // 反射调用会触发方法里的事件；事件是每个对象各自的，没订阅的对象不会触发
            var person = new Person("Owen", 36);
            person.OnCompleted += p => Console.WriteLine($"OnCompleted: {p.Name}");
            MethodInfo? show = typeof(Person).GetMethod("Show");
            show?.Invoke(person, null);                                 // 触发
            show?.Invoke(new Person("Zoe", 10), null);                  // Zoe 没人订阅，不触发
        }
    }
}
