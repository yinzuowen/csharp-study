# C# 入门整理（Java 程序员视角）

> 写给刚入门的 C# 程序员，默认你会一点 Java：先会用、用对，再理解原理。每章都会和 Java 对比。
> 标记说明：🟢 必须掌握（写代码每天都用） · 🟡 进阶（面试常问、排查问题要用） · ⚪ 了解即可（知道有这回事，用到再查）
>
> 环境：macOS + .NET SDK 11 + VS Code（C# Dev Kit、Code Runner）。笔记里的示例都实际跑过，输出是真实结果。

## 仓库结构

```
csharp/
├── README.md     目录（本文件）
├── notes/        知识点整理，按章节，每章一个 md
├── basics/       语言基础练习，单文件，顶层语句写法：dotnet run basics/01-object-var-dynamic.cs
├── advanced/     进阶练习：反射、多线程、Task、async、同步（namespace + Program 类写法）
├── BankDemo/     完整的解决方案：App + Core 类库 + xUnit 测试
└── Hello/        最早的练习草稿（已整理进 basics/ 和 advanced/）
```

## 目录

### 第一部分：语言基础

| 章节 | 内容 | 优先级 |
|---|---|---|
| [第 1 章 类型与基础语法](notes/01-types-and-syntax.md) | 内置类型、var/dynamic、类型转换、const/readonly、可空值类型、enum | 🟢 |
| [第 2 章 值类型与引用类型](notes/02-value-and-reference.md) | struct 与 class、装箱、深浅拷贝、ref/out/in 传参 | 🟢 |
| [第 3 章 类与对象](notes/03-classes.md) | 属性、访问修饰符、构造函数、运算符重载、扩展方法 | 🟢 |
| [第 4 章 继承、接口与多态](notes/04-oop.md) | virtual/override 与 new、abstract、is/as、显式接口实现 | 🟢 |
| [第 5 章 泛型](notes/05-generics.md) | 泛型约束、不擦除、协变与逆变 | 🟢 / 🟡 |
| [第 6 章 委托、Lambda 与事件](notes/06-delegates-events.md) | Func/Action、多播委托、闭包、event | 🟢 |
| [第 7 章 集合与 LINQ](notes/07-collections-linq.md) | List/Dictionary/HashSet、LINQ、延迟执行、yield | 🟢 |
| [第 8 章 异常与资源释放](notes/08-exceptions-resources.md) | 没有受检异常、throw 与 throw ex、using 与 IDisposable | 🟢 |
| [第 9 章 现代 C# 语法](notes/09-modern-syntax.md) | 元组、record、模式匹配、可空引用类型、顶层语句 | 🟢 |

### 第二部分：进阶

| 章节 | 内容 | 优先级 |
|---|---|---|
| [第 10 章 反射与特性](notes/10-reflection-attributes.md) | Type、Activator、Invoke、自定义特性、AssemblyLoadContext | 🟡 |
| [第 11 章 线程与线程池](notes/11-threads.md) | Thread、前台/后台线程、线程池、信号量事件、AsyncLocal | 🟡 |
| [第 12 章 Task、并行与定时器](notes/12-task-parallel.md) | 创建 Task、取消、WaitAll/WhenAll、Parallel、三种 Timer | 🟢 |
| [第 13 章 async/await 与异常](notes/13-async-await.md) | async 执行过程、async void、HttpClient、Task 异常处理 | 🟢 |
| [第 14 章 线程同步](notes/14-synchronization.md) | lock、Interlocked、volatile、SemaphoreSlim、并发集合、Channel | 🟡 |

### 第三部分：工程化

| 章节 | 内容 | 优先级 |
|---|---|---|
| [第 15 章 项目、测试与 NuGet](notes/15-engineering.md) | dotnet CLI、csproj、解决方案、xUnit、Debug 与 Release | 🟢 |
| [第 16 章 常见坑与下一步](notes/16-pitfalls-and-next.md) | 初学者常踩的坑、ASP.NET Core 与 EF Core 学习路线 | 🟢 |

## 学习顺序建议

1. **第 1–9 章按顺序看**，每看完一章，把对应的 `basics/` 练习自己重写一遍。第 7 章（集合与 LINQ）和第 9 章（现代语法）在实际代码里出现得最多，要多练。
2. **第 12、13 章优先于第 10、11 章**：日常写代码几乎只用 Task 和 async/await，直接用 Thread、反射的机会很少。
3. 第 14 章学完后，自己写一个生产者消费者：用 `Channel<T>` 实现，再用 `lock` + `Monitor` 实现一遍，对比两种写法。
4. 第 15 章：给 BankDemo 补测试，然后按第 16 章的路线，把它改成一个 ASP.NET Core Web API。

## 怎么运行练习

```bash
dotnet run basics/06-arrays-linq.cs              # 单文件直接运行（第一次编译较慢）
dotnet build -c Release x.cs -o out && ./out/x   # Release 模式运行，第 14 章 volatile 实验要用
cd BankDemo && dotnet test                       # 运行测试
```

在 VS Code 里用 Code Runner 运行时，有几个常见问题见[第 16 章](notes/16-pitfalls-and-next.md)，例如只运行了选中的代码、`Console.ReadKey` 抛异常。
