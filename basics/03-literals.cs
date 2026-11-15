// 主题：字面量（整数、浮点、字符、字符串）

// ---------- 整数 ----------
Show(123);        // int
Show(123u);       // uint：后缀 u
Show(0x1234L);    // long：后缀 L（小写 l 容易和 1 混淆，推荐大写）
Show(0x1234ul);   // ulong
Show(0123);       // 123！C# 没有八进制字面量，前导 0 被忽略
Show(0b0111_1011);// 二进制 → 123，下划线只是分隔符
Show(Convert.ToInt32("123", 8));  // 真要八进制：用 Convert → 83

// ---------- 浮点 ----------
Show(3.1415926);    // double（默认）
Show(31415926E-7);  // 科学计数法
Show(.5);           // 0.5
Show(1.5f);         // float：后缀 f
Show(1.5m);         // decimal：后缀 m，适合金额

// ---------- 字符：UTF-16，单引号 ----------
Show('a');
Show('\\');         // 转义：反斜杠
Show('\u0061');     // Unicode 编号 → 'a'

// ---------- 字符串 ----------
Show("C:\\");       // 普通字符串，\ 要转义
Show(@"C:\");       // 逐字字符串 @""，\ 不转义
Show(@"hello
world");            // @"" 可以直接换行
Show("hello\nworld");
Show("""
    原始字符串 "引号" 不用转义
    """);

static void Show(object value) => Console.WriteLine($"{value}  ({value.GetType().Name})");
