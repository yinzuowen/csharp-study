#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用



    public class Animal
    {
        // 公开继承
        public void Shout()
        {
            Console.WriteLine("shout");
        }

        // 只有本类和子类（不管子类在不在同一个项目里）
        protected void Eat()
        {
            Console.WriteLine("eat");
        }

        // 本类和子类，或者同一个程序集（项目）里的任何代码
        protected internal void Sleep()
        {
            Console.WriteLine("sleep");
        }
    }

    internal class Cat : Animal
    {

    }

    internal class Dog : Animal
    {

    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Cat cat = new Cat();
            cat.Shout();
            // cat.Eat();
            cat.Sleep();

            Dog dog = new Dog();
            dog.Shout();
        }
    }
}