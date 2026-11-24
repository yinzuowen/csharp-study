#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用


    // as 运算符

    public class Animal
    {
        public string Name { get; set; } = string.Empty;

        public virtual void Shout()
        {
            Console.WriteLine(Name);
        }
    }

    public class Cat : Animal
    {
        public override void Shout()
        {
            Console.WriteLine("Meow");
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            object obj = new Animal();

            Cat cat = obj as Cat;

            if (cat != null)
            {
                Console.WriteLine(cat.ToString());
            }
            else
            {
                Console.WriteLine("Convert failed");
            }
        }
    }
}