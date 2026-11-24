#:property AllowUnsafeBlocks=true

// Console.WriteLine("hello");

// static IEnumerable<(TransactionType type, double amount)> TransactionRecords(string inputText)
// {
//     var reader = new StringReader(inputText);
//     string? line;
//     while ((line = reader.ReadLine()) is not null)
//     {
//         string[] parts = line.Split(',');

//         string? transactionType = parts[0]?.Trim();
//         if (double.TryParse(parts[1].Trim(), out double amount))
//         {
//             // Update the balance based on transaction type
//             if (transactionType?.ToUpper() is "DEPOSIT")
//                 yield return (TransactionType.Deposit, amount);
//             else if (transactionType?.ToUpper() is "WITHDRAWAL")
//                 yield return (TransactionType.Withdrawal, amount);
//         }
//         else {
//             yield return (TransactionType.Invalid, 0.0);
//         }
//     }
// }

// currentBalance += transaction switch
// {
//     (TransactionType.Deposit, var amount) => amount,
//     (TransactionType.Withdrawal, var amount) => -amount,
//     _ => 0.0,
// };

// Console.WriteLine("Hello, World!");

// namespace YourNamespace
// {
//     class YourClass
//     {
//     }

//     struct YourStruct
//     {
//     }

//     interface IYourInterface
//     {
//     }

//     delegate int YourDelegate();

//     enum YourEnum
//     {
//     }
// }


// namespace YourNamespace;

// class YourProgram
// {
//     static void Main(string[] args)
//     {
//         Console.WriteLine("Hello, World!");
//     }
// }

// using System;
// using System.Globalization;

// namespace MyApp.Services;

// class Greeter
// {
//     public string Greet(string name)
//     {
//         var culture = CultureInfo.CurrentCulture;
//         return $"Hello, {name}! Culture: {culture.Name}";
//     }
// }

namespace HelloWorld
{
    // public class Math
    // {
    //     public const double PI = 3.1415926;
    //     public const byte a = 0;
    //     public const sbyte b = 1;
    //     public const short c = 2;
    //     public const short d = 3;
    //     public const long e = 4;
    //     public const char f = '\r';
    //     public const string g = "hello";

    //     public static readonly double PI2 = 0; // 声明时

    //     public Math()
    //     {
    //         PI2 = 3.1415926; // 构造函数里
    //     }

    //     public Math(double d)
    //     {
    //         PI2 = d;
    //     }

    //     // public void Change()
    //     // {
    //     //     PI2 = 3.1415926;
    //     // }
    // }
    internal class Program
    {
        static unsafe void Main(string[] args)
        {
            // Person p = new Person();
            // p.Name = "Owen";
            // p.Age = 38;
            // Console.WriteLine(p.Name + ", " + p.Age);

            // string firstName = "Owen", lastName="Yin";
            // string fullName = firstName + lastName;

            // char[] words = ['O', 'w', 'e', 'n'];
            // string fullName = new string(words);

            // string[] array = ["Owen", "Yin"];
            // string fullName = string.Join(" ", array);

            // Console.WriteLine("fullName: " + fullName);

            // object o1 = new object();
            // object o2 = new object();
            // object o3 = o1;

            // Console.WriteLine("o1 == o2: " + string.Equals(o1, o2));
            // Console.WriteLine("o1 == o3: " + string.Equals(o1, o3));

            // Console.WriteLine("o1.GetHashCode(): " + o1.GetHashCode());
            // Console.WriteLine("o2.GetHashCode(): " + o2.GetHashCode());

            // Console.WriteLine("o1.GetType(): " + o1.GetType());
            // Console.WriteLine("o2.GetType(): " + o2.GetType());

            // object o = 99; // 编译时确定类型

            // Console.WriteLine("o.GetType(): " + o.GetType());

            // o = "test";

            // Console.WriteLine("o.GetType(): " + o.GetType());

            // dynamic d = 99; // 运行时确定类型

            // Console.WriteLine("d.GetType(): " + d.GetType());

            // d = "test";

            // Console.WriteLine("d.GetType(): " + d.GetType());

            // var v = 1; // 隐式类型，编译型推断

            // Console.WriteLine("v.GetType(): " + v.GetType());

            // v = "test";

            // Console.WriteLine("v.GetType(): " + v.GetType());


            /*
             * 隐式类型转换
             * byte 0~255
             * short -32768~32767
             * int -2^31~2^32-1
             */

            // Console.WriteLine(Convert.ToBoolean("True"));

            // string words = "hello.world";
            // float money = 0;

            // bool bool_result = float.TryParse(words, out money);

            // if (bool_result == true)
            // {
            //     Console.WriteLine($"money = {money}, type = {money.GetType()}");
            // }
            // else
            // {
            //     Console.WriteLine($"cannot convert {words} to float");
            // }

            // byte a = 0;
            // byte b = 1;
            // byte c = 2;
            // byte d = 3;
            // byte e = 4;
            // byte f = 5;

            // byte[] array = [a, b, c, d, e, f];

            // var str = BitConverter.ToString(array, 0, array.Length);
            // Console.WriteLine($"str = {str}, type = {str.GetType()}");

            // Console.WriteLine($"BitConverter.IsLittleEndian = {BitConverter.IsLittleEndian}");

            // var bool_result = BitConverter.ToBoolean(array, 1); // 01 -> True
            // Console.WriteLine($"bool_result = {bool_result}, type = {bool_result.GetType()}");

            // var char_result = BitConverter.ToChar(array, 0); // 00 01 -> 0x0100 -> Ā
            // Console.WriteLine($"char_result = {char_result}, type = {char_result.GetType()}");

            // var float_result = BitConverter.ToSingle(array, 0); // 00 01 02 03 -> 0x03020100 -> 3.8204714E-37
            // Console.WriteLine($"float_result = {float_result}, type = {float_result.GetType()}");

            // var short_result = BitConverter.ToInt16(array, 3); // 03 04 -> 0x0403 -> 1027
            // Console.WriteLine($"short_result = {short_result}, type = {short_result.GetType()}");

            // var int_result = BitConverter.ToInt32(array, 0); // 00 01 02 03 -> 0x03020100 -> 50462976
            // Console.WriteLine($"int_result = {int_result}, type = {int_result.GetType()}");

            // string text = "12.34";

            // var float_result = float.Parse(text);
            // Console.WriteLine($"float_result = {float_result}, type = {float_result.GetType()}");

            // var double_result = double.Parse(text);
            // Console.WriteLine($"double_result = {double_result}, type = {double_result.GetType()}");

            // // var short_result = short.Parse(text);
            // var short_result = short.Parse("32767");
            // Console.WriteLine($"short_result = {short_result}, type = {short_result.GetType()}");

            // // var byte_result = byte.Parse(text);
            // var byte_result = byte.Parse("129");
            // Console.WriteLine($"byte_result = {byte_result}, type = {byte_result.GetType()}");

            // // var int_result = int.Parse(text);
            // var int_result = int.Parse("1234");
            // Console.WriteLine($"int_result = {int_result}, type = {int_result.GetType()}");

            // string name = "abc";
            // var t = name.GetType();
            // Console.WriteLine($"name = {name}, type = {t.GetType()}");

            // // string source = "256";
            // string source = "255";
            // bool result = byte.TryParse(source, out byte v);
            // if (result == true)
            // {
            //     Console.WriteLine($"v = {v}, type = {v.GetType()}");
            // }
            // else
            // {
            //     Console.WriteLine($"cannot convert {v} to byte");
            // }

            // var a = 123;
            // Console.WriteLine($"a = {a}, type = {a.GetType()}");

            // var b = 123u;
            // Console.WriteLine($"b = {b}, type = {b.GetType()}");

            // var c = 0x1234l;
            // Console.WriteLine($"c = {c}, type = {c.GetType()}");

            // var d = 0x1234ul;
            // Console.WriteLine($"d = {d}, type = {d.GetType()}");

            // var e = 0123;
            // Console.WriteLine($"e = {e}, type = {e.GetType()}");

            // var h = 3.1415926;
            // var h = 31415926E-7;
            // var h = .5;
            // Console.WriteLine($"h = {h}, type = {h.GetType()}");

            // 字符: unicode utf-16
            // var c = 'a';
            // var c = '\\';
            // var c = '\u0061';
            // Console.WriteLine($"c = {c}, type = {c.GetType()}");

            // 字符串: ""  @""
            // var s = "hello";
            // var s = "C:\\";
            // var s = @"C:\\";
            // var s = @"hello
            // world";
            // var s = "hello\nworld";
            // Console.WriteLine($"s = {s}, type = {s.GetType()}");

            // const 静态常量
            // 内置类型：bool、byte、sbyte、char、decimal、double、float、int、uint、long、ulong、short、ushort以及string和dynamic
            // object：所有类型的基类
            // object 不能用 const 声明为常量
            // const int days = 365;
            // // days = 123; // 不能修改
            // Console.WriteLine($"days = {days}, type = {days.GetType()}");

            // Console.WriteLine($"PI = {Math.PI}");

            // // readonly 动态常量
            // Console.WriteLine($"PI2 = {Math.PI2}");

            // float @class = 0;

            // Console.WriteLine(Add(1, 2));

            // Person p = new Person();

            // Console.WriteLine($"Person.PI = {Person.PI}");
            // Person.PI = 3.14;
            // Console.WriteLine($"Person.PI = {Person.PI}");

            // Person p = new Person("Owen");
            // Console.WriteLine($"p.Name = {p.Name}");

            // int a = 100;
            // int b = a;
            // ref int c = ref a;
            // Console.WriteLine($"a={a}, b={b}, c={c}");
            // a = 200;
            // Console.WriteLine($"a={a}, b={b}, c={c}");
            // b = 300;
            // Console.WriteLine($"a={a}, b={b}, c={c}");
            // c = 400;
            // Console.WriteLine($"a={a}, b={b}, c={c}");

            // int? x = 123;
            // int y = x ?? 100;
            // Console.WriteLine($"y={y}");

            // * 取值
            // & 取地址
            // is 类型判断
            // sizeof 取大小
            // typeof 取类型
            // int a = 123;
            // int* ptr = &a;

            // Console.WriteLine($"a={a}");
            // Console.WriteLine($"*ptr={*ptr}");
            // Console.WriteLine($"ptr={(int)ptr}");

            // if (a is int)
            // {
            //     Console.WriteLine("a is int");
            // }

            // object obj = "hello";
            // string text = obj as string;
            // Console.WriteLine($"text={text}");

            // Console.WriteLine($"sizeof(int)={sizeof(int)}");
            // Console.WriteLine($"typeof(int)={typeof(int)}");

            // int[] array1;
            // Console.WriteLine($"array1.Length = {array1.Length}");

            // int[] array2 = new int[5] { 1, 2, 3, 4, 5 };
            // Console.WriteLine($"array2.Length = {array2.Length}");
            // Console.WriteLine($"array2.LastOrDefault = {array2.LastOrDefault()}");

            // var array3 = array2;
            // Console.WriteLine(array2 == array3);

            // // var cloned = (int[])array2.Clone();
            // var cloned = array2.ToArray();
            // Console.WriteLine(array2 == cloned);
            // Array.ForEach(cloned, (item) => Console.WriteLine($"{item}"));

            // var group = array2.GroupBy(item => item % 2 == 0);
            // Console.WriteLine(group);

            // foreach (var g in group)
            // {
            //     Console.WriteLine($"Key = {g.Key}: {string.Join(", ", g)}");
            // }

            // int[][] selected = array2.Select(item => new int[] { item, item * 10 }).ToArray();

            // Console.WriteLine($"selected: {string.Join(", ", selected)}");

            // foreach (var p in selected)
            // {
            //     Console.WriteLine(string.Join(", ", p));
            // }

            // int[] selectedMany = array2.SelectMany(item => new int[] { item, item * 10 }).ToArray();

            // Console.WriteLine($"selectedMany: {string.Join(", ", selectedMany)}");

            // 交错数组
            // int[][] array = new int[5][];
            // array[0] = new int[] { 1 };
            // array[1] = new int[] { 2, 3 };
            // array[2] = new int[] { 4, 5, 6 };
            // array[3] = new int[] { 7, 8, 9, 10 };
            // array[4] = new int[] { 11, 12, 13, 14, 15 };

            // for (int i = 0; i < array.Length; i++)
            // {
            //     for (int j = 0; j < array[i].Length; j++)
            //     {
            //         Console.Write($"{array[i][j]} ");
            //     }
            //     Console.WriteLine();
            // }

            // enum 枚举类型，隐式继承于 int
            // Color c = Color.Red;

            // Console.WriteLine(c);

            // Type colorType = typeof(Color);

            // Console.WriteLine($"BaseType: {colorType.BaseType}");
            // Console.WriteLine($"GetUnderlyingType(): {Enum.GetUnderlyingType(colorType)}");
            // Console.WriteLine($"GetName(): {Enum.GetName(colorType, 1)}");

            // string[] names = Enum.GetNames(colorType);
            // Console.WriteLine(string.Join(", ", names));

            // Array values = Enum.GetValues(colorType);

            // foreach (var value in values)
            // {
            //     Console.Write($"{value} ");
            // }

            // Console.WriteLine();

            // Color red = (Color)Enum.Parse(colorType, "Red");

            // bool isParsed = Enum.TryParse<Color>("Red", out Color colorResult);

            // if (isParsed)
            // {
            //     Console.WriteLine($"colorResult: {colorResult}");
            // }
            // else
            // {
            //     Console.WriteLine("cannot convert");
            // }

            // object obj = Enum.ToObject(colorType, 1);
            // Console.WriteLine($"obj: {obj}");

            // Color color = (Color)2;
            // Console.WriteLine($"color: {color}");

            // for (int j = 0; j < 8; j++)
            // {
            //     Console.WriteLine("{0,1} - {1:G}", j, (MultiRole)j);
            // }

            Point point = new Point(100, 200);
            // point.X = 100;
            // point.Y = 200;

            Write(point);
            Write(point.X);
            Write(point.Y);
            // Console.WriteLine(point.ToString());

            DoSomething(ref point);

            Write(point);

            // 装箱
            // int num = 123;
            // object obj = num;
            // Console.WriteLine(obj);

            // 拆箱
            // object obj = 456;
            // int num = (int)obj;
            // Console.WriteLine(num);


        }

        static void Write(ValueType param)
        {
            Console.WriteLine(param);
        }

        // 值传递
        // static void DoSomething(Point point)
        // {
        //     point.X = 300;
        //     point.Y = 400;
        //     Write(point);
        // }

        // 引用传递
        static void DoSomething(ref Point point)
        {
            point.X = 300;
            point.Y = 400;
            Write(point);
        }

        static int Add(int x, int y)
        {
            return x + y;
        }
    }

    public class ClassName
    {
        public ClassName()
        {

        }

        // 析构函数
        ~ClassName()
        {

        }

        public const intConstName ConstName = 123;

        private string fieldName = string.Empty;

        public string PropertyName { get; set; }

        public string this[int i]
        {
            get { return null; }
            set { }
        }

        public void MethodName(string args)
        {
            
        }

        public event EventHandler MethodNameChanged;
    }

    // 值类型的基类是 System.ValueType
    // 引用类型的基类是 System.Object
    // System.ValueType 的基类是 System.Object
    // 结构体的基类是 System.ValueType => System.Object
    internal struct Point
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override readonly string ToString()
        {
            return $"Point(X={X}, Y={Y})";
        }
    }

    internal enum Color
    {
        Red,
        Green,
        Blue
    }

    [Flags]
    public enum MultiRole
    {
        Insert = 0,
        Delete = 1,
        Update = 2,
        Select = 4
    }

    internal class Person
    {

        public static double PI = 3.1416;

        private string name = string.Empty;

        public Person()
        {
            Console.WriteLine($"Hello, I'm {name}");
        }

        public Person(string name)
        {
            Name = name;
            Console.WriteLine($"Hello, I'm {name}");
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        private int age = 0;

        public int Age
        {
            get
            {
                return age;
            }
            set
            {
                age = value;
            }
        }
    }
}
