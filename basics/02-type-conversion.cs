// 主题：类型转换（Convert / Parse / TryParse / BitConverter）
//
// 隐式类型转换：小范围 → 大范围自动转换，不会丢数据
// byte  0 ~ 255
// short -32768 ~ 32767
// int   -2^31 ~ 2^31-1

// ---------- Convert ----------
Console.WriteLine(Convert.ToBoolean("True"));

// ---------- Parse：失败会抛异常 ----------
string text = "12.34";
Console.WriteLine($"float.Parse  = {float.Parse(text)}");
Console.WriteLine($"double.Parse = {double.Parse(text)}");
Console.WriteLine($"short.Parse  = {short.Parse("32767")}");   // 超过 32767 抛 OverflowException
Console.WriteLine($"byte.Parse   = {byte.Parse("129")}");      // 超过 255 抛 OverflowException
Console.WriteLine($"int.Parse    = {int.Parse("1234")}");      // int.Parse("12.34") 抛 FormatException

// ---------- TryParse：失败返回 false，不抛异常 ----------
string words = "hello.world";
if (float.TryParse(words, out float money))
    Console.WriteLine($"money = {money}");
else
    Console.WriteLine($"cannot convert {words} to float");

string source = "256";
if (byte.TryParse(source, out byte b))
    Console.WriteLine($"b = {b}");
else
    Console.WriteLine($"cannot convert {source} to byte");     // 256 超出 byte 范围

// ---------- BitConverter：把字节按不同类型重新解读 ----------
byte[] bytes = [0, 1, 2, 3, 4, 5];
Console.WriteLine($"bytes = {BitConverter.ToString(bytes)}");             // 00-01-02-03-04-05
Console.WriteLine($"IsLittleEndian = {BitConverter.IsLittleEndian}");    // 小端序：低位字节在前

Console.WriteLine($"ToBoolean(1) = {BitConverter.ToBoolean(bytes, 1)}");  // 01 → 非 0 → True
Console.WriteLine($"ToChar(0)    = {BitConverter.ToChar(bytes, 0)}");     // 00 01 → 0x0100 → Ā
Console.WriteLine($"ToSingle(0)  = {BitConverter.ToSingle(bytes, 0)}");   // 00 01 02 03 → 3.8204714E-37
Console.WriteLine($"ToInt16(3)   = {BitConverter.ToInt16(bytes, 3)}");    // 03 04 → 0x0403 → 1027
Console.WriteLine($"ToInt32(0)   = {BitConverter.ToInt32(bytes, 0)}");    // 00 01 02 03 → 0x03020100 → 50462976
