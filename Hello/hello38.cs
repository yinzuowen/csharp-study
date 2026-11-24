#:property PublishAot=false

namespace HelloWorld
{
    // 值类型：enum、struct（含 int/double/bool/char 等内置类型、record struct）
    // 引用类型：class（含 object/string、record）、interface、delegate、数组
    // 指针类型：int* 等，只能在 unsafe 代码中使用

    internal class EntityBase
    {
        public int Id { get; set; }
    }

    internal class UserEntity : EntityBase
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            UserEntity entity = new UserEntity();
            entity.Id = 1;
            entity.Username = "Owen";
            entity.Password = "Yin";
        }
    }
}