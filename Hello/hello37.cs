#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    // 泛型的逆变性

    internal interface IService<in T> { }

    internal class Service<T> : IService<T> { }

    internal class Program
    {
        static void Main(string[] args)
        {
            object obj = "Owen";
            string name = (string)obj;

            IService<string> strService = new Service<string>();
            IService<object> objService = new Service<object>();

            // objService = strService;
            strService = objService;
        }
    }
}