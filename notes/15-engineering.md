# 第 15 章 项目、测试与 NuGet 🟢

> 本章练习：[BankDemo.slnx](../BankDemo/BankDemo.slnx)（[BankDemo.App](../BankDemo/BankDemo.App/Program.cs)、[BankDemo.Core](../BankDemo/BankDemo.Core/Models/BankAccount.cs)、[BankDemo.Tests](../BankDemo/BankDemo.Tests/BankAccountTests.cs)）

> 前面各章都是一个 `.cs` 文件 `dotnet run` 就跑。真写项目时你会遇到这些问题：代码多了要拆成几个项目，怎么互相引用？第三方库（相当于 Maven 依赖）怎么加？单元测试怎么写、怎么跑？`bin/`、`obj/` 要不要提交？为什么 Debug 下好好的程序 Release 下卡死？编译输出里一堆 `CS8602`、`CA1822`、`IDE0161` 是什么，能不能关？
>
> 本章以 BankDemo 解决方案为例，讲 dotnet CLI、项目文件、解决方案、NuGet、xUnit 测试和编译配置。Java 里 Maven/Gradle 做的事情，在 .NET 里由 dotnet CLI + MSBuild 完成。
>
> 本章示例都在 .NET 11 上实际跑过，输出是真实结果。（BankDemo 的 `TargetFramework` 是 `net10.0`，本机同时装了 .NET 10 运行时，所以测试输出里显示的是 `net10.0`。）

## 1. dotnet CLI 常用命令 🟢

`dotnet` 一个命令包办了 Maven 的 `archetype:generate`、`compile`、`exec`、`test`、依赖管理。

| 要做的事 | dotnet CLI | Maven / Gradle |
|---|---|---|
| 新建控制台项目 | `dotnet new console -o MyApp` | `mvn archetype:generate` / `gradle init` |
| 新建类库 | `dotnet new classlib -o MyLib` | 新建一个 jar 模块 |
| 新建 xUnit 测试项目 | `dotnet new xunit -o MyApp.Tests` | 加 JUnit 依赖的测试模块 |
| 新建解决方案 | `dotnet new sln -n MyApp` | 父 `pom.xml`（`<modules>`）/ `settings.gradle` |
| 把项目加进解决方案 | `dotnet sln add MyLib/MyLib.csproj` | `<module>MyLib</module>` / `include 'MyLib'` |
| 项目引用另一个项目 | `dotnet add MyApp reference MyLib` | `<dependency>` 指向兄弟模块 / `implementation project(':MyLib')` |
| 加第三方包 | `dotnet add package Newtonsoft.Json` | `<dependency>` / `implementation '...'` |
| 还原依赖 | `dotnet restore`（build/run/test 会自动做） | `mvn dependency:resolve` |
| 编译 | `dotnet build`（`-c Release`） | `mvn compile` / `gradle build` |
| 运行 | `dotnet run --project MyApp` | `mvn exec:java` / `gradle run` |
| 跑测试 | `dotnet test` | `mvn test` / `gradle test` |
| 发布（打出可部署的文件） | `dotnet publish -c Release` | `mvn package` |
| 清理 | `dotnet clean` | `mvn clean` |
| 查看依赖 | `dotnet list package` | `mvn dependency:tree` |

.NET 10 起还多了一套"名词在前"的新写法，老写法仍然能用：

| 老写法 | 新写法 |
|---|---|
| `dotnet add package X` | `dotnet package add X` |
| `dotnet add reference P` | `dotnet reference add P` |
| `dotnet sln add P` | `dotnet solution add P` |
| `dotnet list package` | `dotnet package list` |

`dotnet package add --help` 里能看到版本可以用 `@` 指定：`dotnet package add Newtonsoft.Json@13.0.3`。

在 BankDemo 目录下实测几个查看命令：

```bash
cd BankDemo
dotnet sln list
dotnet list BankDemo.App reference
dotnet list BankDemo.Tests package
```
```
Project(s)
----------
BankDemo.App/BankDemo.App.csproj
BankDemo.Core/BankDemo.Core.csproj
BankDemo.Tests/BankDemo.Tests.csproj

Project reference(s)
--------------------
../BankDemo.Core/BankDemo.Core.csproj

Project 'BankDemo.Tests' has the following package references
   [net10.0]: 
   Top-level Package                Requested   Resolved
   > coverlet.collector             6.0.4       6.0.4   
   > Microsoft.NET.Test.Sdk         17.14.1     17.14.1 
   > xunit                          2.9.3       2.9.3   
   > xunit.runner.visualstudio      3.1.4       3.1.4   
```

从零搭一个和 BankDemo 一样结构的解决方案，命令是这样的：

```bash
dotnet new sln -n BankDemo
dotnet new classlib -o BankDemo.Core
dotnet new console  -o BankDemo.App
dotnet new xunit    -o BankDemo.Tests
dotnet sln add BankDemo.Core BankDemo.App BankDemo.Tests
dotnet add BankDemo.App   reference BankDemo.Core
dotnet add BankDemo.Tests reference BankDemo.Core
dotnet new gitignore
```

## 2. file-based app 和项目 🟢

### 2.1 file-based app：一个 .cs 文件就能跑

`basics/`、`advanced/` 里的练习都是 file-based app：没有 `.csproj`，直接 `dotnet run xxx.cs`。需要的配置用文件开头的 `#:` 指令写：

| 指令 | 作用 | 对应 csproj 里的 |
|---|---|---|
| `#:package Newtonsoft.Json@13.0.3` | 引用 NuGet 包 | `<PackageReference>` |
| `#:property PublishAot=false` | 设置 MSBuild 属性 | `<PropertyGroup>` 里的一项 |
| `#:project ../MyLib/MyLib.csproj` | 引用一个项目 | `<ProjectReference>` |
| `#:sdk Microsoft.NET.Sdk.Web` | 换 SDK（比如写 Web 程序） | `<Project Sdk="...">` |

例子：

```csharp
#:package Newtonsoft.Json@13.0.3
#:property PublishAot=false
#:property NuGetAudit=false
using Newtonsoft.Json;

var obj = new { Name = "Owen", Balance = 10200.00m };
Console.WriteLine(JsonConvert.SerializeObject(obj));
Console.WriteLine($"运行时：{Environment.Version}");
```
```
{"Name":"Owen","Balance":10200.00}
运行时：11.0.0
```

`NuGetAudit=false` 是因为本机连不上 nuget.org：默认还原时会去查包的安全漏洞，连不上会白白等好几分钟，最后报 `warning NU1900`。网络正常时不用加。

file-based app 的编译输出不在文件旁边，而是放在用户目录下（macOS 上是 `~/Library/Application Support/dotnet/runfile/`），所以练习目录里不会出现 `bin/`、`obj/`。

### 2.2 file-based app 背后其实也是一个项目

`dotnet project convert` 能把单文件转成项目，从中可以看到它的默认配置。对一个只有一行 `Console.WriteLine("hi");` 的文件实测：

```bash
dotnet project convert plain.cs -o conv2
```
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net11.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PublishAot>true</PublishAot>
    <PackAsTool>true</PackAsTool>
    <UserSecretsId>plain-3300543a...</UserSecretsId>
  </PropertyGroup>
</Project>
```

注意 **`PublishAot` 默认是 `true`**。开了 AOT 之后，反射式的 JSON 序列化会被禁用，这就是练习文件第一行都写 `#:property PublishAot=false` 的原因（详见[第 16 章](16-pitfalls-and-next.md)）。

### 2.3 什么时候该建项目

| | file-based app | 项目（.csproj） |
|---|---|---|
| 文件数 | 一个 `.cs` | 任意多个，按目录组织 |
| 适合 | 练习、试 API、小脚本 | 正式程序、要拆模块、要写测试 |
| 引用别的代码 | `#:project`、`#:package` | `ProjectReference`、`PackageReference` |
| 单元测试 | 不方便 | 单独的测试项目 |
| 类比 Java | `java Hello.java`（JDK 11+ 的单文件运行） | Maven 工程 |

满足下面任意一条就建项目：代码超过一个文件、需要被别的代码引用、需要写测试、要交给别人部署。单文件写大了可以直接 `dotnet project convert` 转过去。

⚠️ VS Code 的 Code Runner 配置里，如果当前目录有 `.csproj` 就跑 `dotnet run`（跑项目），否则跑 `dotnet run 文件名`。所以**不要把单文件练习放进项目目录**，否则运行的是项目而不是你打开的文件。

## 3. csproj 关键属性 🟢

`.csproj` 相当于 `pom.xml`，但简洁得多：源文件不用列，目录下所有 `.cs` 自动参与编译。BankDemo.App 的项目文件：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\BankDemo.Core\BankDemo.Core.csproj" />
  </ItemGroup>

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

| 属性 | 作用 | 常见值 |
|---|---|---|
| `Sdk` | 用哪套构建规则 | `Microsoft.NET.Sdk`（控制台、类库）、`Microsoft.NET.Sdk.Web`（ASP.NET Core） |
| `OutputType` | 输出类型 | `Exe`：可执行程序；不写就是 `Library`（类库，相当于 jar） |
| `TargetFramework` | 目标框架 | `net10.0`、`net11.0`；需要多个时写 `TargetFrameworks`（复数）用分号分隔 |
| `ImplicitUsings` | 自动加常用 `using` | `enable` |
| `Nullable` | 可空引用类型检查 | `enable`：`string` 不能为 null，`string?` 才可以（见[第 9 章](09-modern-syntax.md)） |
| `IsPackable` | 能否打成 NuGet 包 | 测试项目设成 `false` |
| `InternalsVisibleTo` | 让指定程序集能访问 `internal` 成员 | 见 4.2 |

`ImplicitUsings=enable` 时，编译器生成一个 `obj/.../*.GlobalUsings.g.cs`。在 .NET 11 控制台项目上实测内容：

```csharp
// <auto-generated/>
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Net.Http.Json;
global using System.Threading;
global using System.Threading.Tasks;
```

⚠️ `System.Text`（`StringBuilder`、`Encoding`）、`System.Text.Json`、`System.Diagnostics`（`Stopwatch`）、`System.Reflection` 都**不在**里面，要自己写 `using`。

还可以在 csproj 里加自己的全局 using。BankDemo.Tests 就加了 `Xunit`，所以测试文件里不用写 `using Xunit;`：

```xml
<ItemGroup>
  <Using Include="Xunit" />
</ItemGroup>
```

## 4. 解决方案、分层与程序集 🟢

### 4.1 解决方案和项目引用

解决方案（solution）把多个项目组织在一起，相当于 Maven 的父工程。现在 `dotnet new sln` 生成的是 XML 格式的 `.slnx`（实测 .NET 11 SDK），比老的 `.sln` 好读得多：

```xml
<Solution>
  <Project Path="BankDemo.App/BankDemo.App.csproj" />
  <Project Path="BankDemo.Core/BankDemo.Core.csproj" />
  <Project Path="BankDemo.Tests/BankDemo.Tests.csproj" />
</Solution>
```

在解决方案目录下执行 `dotnet build`、`dotnet test`，会对里面所有项目生效。

BankDemo 的分层：

```
BankDemo.App   (Exe)      控制台入口：读数据、调用 Core、打印结果
    │ ProjectReference
    ▼
BankDemo.Core  (Library)  业务逻辑：BankAccount、Transaction、CsvTransactionParser
    ▲ ProjectReference
    │
BankDemo.Tests (xUnit)    测试 Core
```

| 项目 | 职责 | Java 对应 |
|---|---|---|
| `BankDemo.Core` | 模型、业务规则、接口，不关心输入输出 | `xxx-core` 模块（jar） |
| `BankDemo.App` | 程序入口，组装依赖 | `xxx-app` 模块（带 `main` 的可执行 jar） |
| `BankDemo.Tests` | 单元测试 | `src/test/java`，但 .NET 里测试是**单独的项目** |

规则：依赖只能单向（App → Core，Tests → Core），Core 不能引用 App。实测：`dotnet add BankDemo.Core reference BankDemo.App` 本身不会拦你，但之后一编译就报错：

```
error MSB4006: There is a circular dependency in the target dependency graph involving target "_GenerateRestoreProjectPathWalk".
```

### 4.2 程序集、命名空间与 internal

| 概念 | C# | Java |
|---|---|---|
| 编译产物 | 程序集（assembly），即 `.dll` | `.jar` |
| 代码组织 | 命名空间 `namespace BankDemo.Core.Models;` | 包 `package bankdemo.core.models;` |
| 和目录的关系 | 习惯上一致，但**不强制** | 必须一致 |

一个项目编译成一个程序集。BankDemo.App 编译后，Core 的 dll 会被复制到 App 的输出目录：

```
$ ls BankDemo.App/bin/Debug/net10.0/ | grep BankDemo
BankDemo.App
BankDemo.App.deps.json
BankDemo.App.dll
BankDemo.App.pdb
BankDemo.App.runtimeconfig.dev.json
BankDemo.App.runtimeconfig.json
BankDemo.Core.dll
BankDemo.Core.pdb
```

`internal` 的边界是**程序集**，不是命名空间。这一点和 Java 的包访问权限（不写修饰符）不一样：

| C# | 可见范围 | Java 近似 |
|---|---|---|
| `public` | 任何地方 | `public` |
| `internal` | 同一个程序集（同一个项目） | 包访问权限，但范围是整个项目 |
| `protected` | 自己和子类 | `protected`（但 Java 的还包括同包） |
| `private` | 只有自己 | `private` |

`AmountValidator` 是 `internal static class`。把 App 里那行注释取消掉再编译，实测：

```
BankDemo.App/Program.cs(58,1): error CS0122: 'AmountValidator' is inaccessible due to its protection level
Build FAILED.
```

但测试项目要测它，所以 Core 的 csproj 里开了个口子：

```xml
<ItemGroup>
  <InternalsVisibleTo Include="BankDemo.Tests" />
</ItemGroup>
```

这样 `CsvTransactionParserTests.AmountValidator_IsVisibleToTests` 就能直接调用 `AmountValidator.EnsurePositive(0)`。

建议：类库里**默认写 `internal`**，确实要给别的项目用的才写 `public`。（`dotnet new classlib` 生成的 `Class1` 是 `public class`，这只是模板，不代表推荐。）

## 5. NuGet 包管理 🟢

NuGet 是 .NET 的包仓库（nuget.org），相当于 Maven Central。

### 5.1 加一个包

```bash
dotnet add package Newtonsoft.Json                # 最新稳定版
dotnet add package Newtonsoft.Json --version 13.0.3
dotnet package add Newtonsoft.Json@13.0.3         # 新写法
```

执行后 csproj 里多一行 `PackageReference`：

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
</ItemGroup>
```

| | NuGet | Maven |
|---|---|---|
| 声明依赖 | `<PackageReference Include="xunit" Version="2.9.3" />` | `<dependency><groupId>..</groupId><artifactId>..</artifactId><version>..</version></dependency>` |
| 包的标识 | 只有一个包名（`xunit`） | `groupId:artifactId` |
| 本地缓存 | `~/.nuget/packages/` | `~/.m2/repository/` |
| 传递依赖 | 自动带进来 | 自动带进来 |
| 测试范围依赖 | 没有 `scope`，直接加在测试项目里 | `<scope>test</scope>` |
| 统一管理版本 | `Directory.Packages.props`（中央包管理） | 父 pom 的 `<dependencyManagement>` |

BankDemo.Tests 用到的包：

| 包 | 作用 |
|---|---|
| `xunit` | 测试框架本体：`[Fact]`、`Assert` |
| `xunit.runner.visualstudio` | 让 `dotnet test` 和 IDE 能发现、运行 xUnit 测试 |
| `Microsoft.NET.Test.Sdk` | 测试宿主，`dotnet test` 需要它 |
| `coverlet.collector` | 代码覆盖率（`dotnet test --collect:"XPlat Code Coverage"`） |

### 5.2 版本 ⚠️

- `Version="13.0.3"` 在 NuGet 里的意思是**最低 13.0.3**，不是"只能 13.0.3"。没有冲突时用的就是这个版本。
- 包是按需还原的：`dotnet build`、`run`、`test` 第一次都会先 restore，下载到 `~/.nuget/packages/`，之后离线也能用。
- 实测：本机连不上 nuget.org，但 BankDemo 用到的包都已经在本地缓存里，所以测试照样能跑，只是 restore 阶段先卡了 7 分钟（`Restored ... BankDemo.Tests.csproj (in 7.03 min)`），并报 `warning NU1900: Error occurred while getting package vulnerability data`。这个警告来自安全审计，可以用 `-p:NuGetAudit=false` 关掉。

## 6. xUnit 单元测试 🟢

.NET 常见的测试框架有 xUnit、NUnit、MSTest，`dotnet new xunit` 建的就是 xUnit。

### 6.1 [Fact] 和 [Theory]

| xUnit | JUnit | 说明 |
|---|---|---|
| `[Fact]` | `@Test` | 一个测试用例 |
| `[Theory]` + `[InlineData(...)]` | `@ParameterizedTest` + `@ValueSource` | 同一个测试用多组数据跑 |
| 构造函数 | `@Before` | xUnit **每个测试都新建一个测试类实例**，构造函数里做准备 |
| `IDisposable.Dispose` | `@After` | 收尾 |
| `[Fact(Skip = "原因")]` | `@Ignore` | 跳过 |

BankDemo 里的例子：

```csharp
public class BankAccountTests
{
    [Fact]
    public void Deposit_IncreasesBalance()
    {
        var account = new BankAccount("Owen");    // Arrange：准备

        account.Deposit(100m);                    // Act：执行

        Assert.Equal(100m, account.Balance);      // Assert：断言
        Assert.Single(account.Transactions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_NonPositiveAmount_Throws(decimal amount)
    {
        var account = new BankAccount("Owen");

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount));
    }
}
```

测试方法按 **Arrange / Act / Assert** 三段写，中间空一行。

测试命名常用 `被测方法_场景_预期结果`，比如 `Withdraw_MoreThanBalance_Throws`。失败时只看名字就知道哪条规则坏了。

### 6.2 常用 Assert

| 方法 | 用途 | JUnit 对应 |
|---|---|---|
| `Assert.Equal(expected, actual)` | 相等（**先写期望值**） | `assertEquals` |
| `Assert.NotEqual` | 不相等 | `assertNotEquals` |
| `Assert.True(x)` / `Assert.False(x)` | 布尔 | `assertTrue` / `assertFalse` |
| `Assert.Null(x)` / `Assert.NotNull(x)` | null | `assertNull` / `assertNotNull` |
| `Assert.Same` | 同一个对象（引用相等） | `assertSame` |
| `Assert.Empty(c)` / `Assert.Single(c)` | 集合为空 / 恰好一个元素 | — |
| `Assert.Contains(item, c)` | 集合包含 | — |
| `Assert.Throws<T>(() => ...)` | 抛出**恰好是** `T` 类型的异常，并返回这个异常 | `assertThrows` |
| `await Assert.ThrowsAsync<T>(() => ...)` | 异步方法抛异常 | — |

⚠️ `Assert.Throws<T>` 要求异常类型**完全一致**，抛子类也算失败；允许子类用 `Assert.ThrowsAny<T>`。

`Assert.Throws` 返回异常对象，可以接着检查消息：

```csharp
var ex = Assert.Throws<InvalidOperationException>(() => account.Withdraw(100m));
Assert.Contains("余额不足", ex.Message);
```

record 按值比较，所以可以直接拿整个对象断言（`CsvTransactionParserTests` 里的写法）：

```csharp
Assert.Equal(new Transaction(TransactionType.Deposit, 100m, "salary"), result[0]);
```

### 6.3 运行 dotnet test

在 BankDemo 目录下实测：

```bash
dotnet test
```
```
  BankDemo.Core -> .../BankDemo.Core/bin/Debug/net10.0/BankDemo.Core.dll
  BankDemo.Tests -> .../BankDemo.Tests/bin/Debug/net10.0/BankDemo.Tests.dll
Test run for .../BankDemo.Tests/bin/Debug/net10.0/BankDemo.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 29 ms - BankDemo.Tests.dll (net10.0)
```

源码里只有 7 个测试方法，结果是 10 个：5 个 `[Fact]`，加上两个 `[Theory]` 分别有 2 组、3 组数据，每组算一个用例。想看每个用例的名字：

```bash
dotnet test --logger "console;verbosity=normal"
```
```
  Passed BankDemo.Tests.CsvTransactionParserTests.AmountValidator_IsVisibleToTests [3 ms]
  Passed BankDemo.Tests.BankAccountTests.Withdraw_MoreThanBalance_Throws [4 ms]
  Passed BankDemo.Tests.BankAccountTests.Deposit_NonPositiveAmount_Throws(amount: 0) [< 1 ms]
  Passed BankDemo.Tests.BankAccountTests.Deposit_NonPositiveAmount_Throws(amount: -10) [< 1 ms]
  Passed BankDemo.Tests.BankAccountTests.Deposit_IncreasesBalance [< 1 ms]
  Passed BankDemo.Tests.CsvTransactionParserTests.Parse_SkipsBlankLines [1 ms]
  Passed BankDemo.Tests.CsvTransactionParserTests.Parse_BadLine_ReturnsInvalid(line: "REFUND, 100, x") [1 ms]
  Passed BankDemo.Tests.CsvTransactionParserTests.Parse_BadLine_ReturnsInvalid(line: "DEPOSIT") [< 1 ms]
  Passed BankDemo.Tests.CsvTransactionParserTests.Parse_BadLine_ReturnsInvalid(line: "DEPOSIT, abc") [< 1 ms]
  Passed BankDemo.Tests.CsvTransactionParserTests.Parse_ValidLines_ReturnsTransactions [1 ms]

Test Run Successful.
Total tests: 10
     Passed: 10
```

xUnit 默认让不同测试类**并行**执行，所以顺序每次可能不同。测试之间不要共享可变状态。

只跑一部分：

```bash
dotnet test --filter "FullyQualifiedName~BankAccountTests"
```
```
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 12 ms - BankDemo.Tests.dll (net10.0)
```

测试失败时长什么样？在副本里临时加一个故意写错期望值的测试（存 100 取 30，断言余额是 60）：

```
[xUnit.net 00:00:00.14]     BankDemo.Tests.FailDemoTests.Withdraw_ReducesBalance_Wrong [FAIL]
  Failed BankDemo.Tests.FailDemoTests.Withdraw_ReducesBalance_Wrong [8 ms]
  Error Message:
   Assert.Equal() Failure: Values differ
Expected: 60
Actual:   70
  Stack Trace:
     at BankDemo.Tests.FailDemoTests.Withdraw_ReducesBalance_Wrong() in BankDemo.Tests/FailDemoTests.cs:line 11

Failed!  - Failed:     1, Passed:    10, Skipped:     0, Total:    11, Duration: 45 ms - BankDemo.Tests.dll (net10.0)
```

这也说明 `Assert.Equal` 为什么要**先写期望值**：写反了，报错里的 Expected 和 Actual 就是反的。

顺便运行一下 App：

```bash
dotnet run --project BankDemo.App
```
```
Deposit    10,000.00  余额  10,000.00  Initial balance
Withdrawal  1,000.00  余额   9,000.00  rent
Deposit     2,000.00  余额  11,000.00  freelance payment
Withdrawal    300.00  余额  10,700.00  groceries
跳过无效记录：unknown type
跳过无效记录：DEPOSIT,     abc, bad amount
Withdrawal    500.00  余额  10,200.00  car maintenance

Owen 的账户，余额 10,200.00
总收入 12,000.00，总支出 1,800.00
```

## 7. Debug 和 Release 🟢

`dotnet build`、`dotnet run` 默认是 **Debug** 配置，加 `-c Release` 切换：

```bash
dotnet build -c Release
```
```
  BankDemo.Core -> .../BankDemo.Core/bin/Release/net10.0/BankDemo.Core.dll
  BankDemo.App -> .../BankDemo.App/bin/Release/net10.0/BankDemo.App.dll
  BankDemo.Tests -> .../BankDemo.Tests/bin/Release/net10.0/BankDemo.Tests.dll
```

| | Debug | Release |
|---|---|---|
| 输出目录 | `bin/Debug/<框架>/` | `bin/Release/<框架>/` |
| 编译器优化 | 关 | 开（`Optimize=true`） |
| JIT 优化 | 禁用（方便断点、看变量） | 启用 |
| 条件编译符号 | `DEBUG` | `RELEASE` |
| `Debug.Assert`、`Debug.WriteLine` | 生效 | **整行被编译器去掉** |
| 用途 | 本地开发、调试 | 部署、性能测试 |

用一个小程序实测两种配置的区别：

```csharp
using System.Diagnostics;
using System.Reflection;

#if DEBUG
Console.WriteLine("定义了 DEBUG");
#endif
#if RELEASE
Console.WriteLine("定义了 RELEASE");
#endif
var opt = Assembly.GetEntryAssembly()!.GetCustomAttribute<DebuggableAttribute>();
Console.WriteLine($"JIT 优化被禁用：{opt?.IsJITOptimizerDisabled ?? false}");
Debug.Assert(1 + 1 == 3, "Debug.Assert 失败");
Console.WriteLine("走到了最后一行");
```

`dotnet run config.cs`（Debug）：

```
定义了 DEBUG
JIT 优化被禁用：True
Process terminated.
Assertion failed.
Debug.Assert 失败
   at Program.<Main>$(String[] args) in .../config.cs:line 13
```

`dotnet run -c Release config.cs`：

```
定义了 RELEASE
JIT 优化被禁用：False
走到了最后一行
```

⚠️ 优化会暴露多线程代码里的 bug：一个线程 `while (flag)` 循环，另一个线程把 `flag` 改成 `false`，Debug 下循环能停，Release 下 JIT 可能把 `flag` 的读取提到循环外，**循环永远不结束**。实测见[第 14 章](14-synchronization.md)的 volatile 一节和[第 16 章](16-pitfalls-and-next.md)。所以**性能测试和多线程测试一定要用 Release 跑一遍**。

## 8. .gitignore：bin/ 和 obj/ 🟢

| 目录 | 内容 | 要不要提交 |
|---|---|---|
| `bin/` | 编译输出：dll、exe、pdb | ❌ |
| `obj/` | 中间文件：restore 结果 `project.assets.json`、生成的 `GlobalUsings.g.cs` 等 | ❌ |
| `.vs/`、`.idea/` | IDE 本地状态 | ❌ |
| `*.csproj`、`*.slnx`、`.editorconfig` | 项目配置 | ✅ |

相当于 Maven 的 `target/` 不提交。不用自己写，直接生成官方模板：

```bash
dotnet new gitignore
```

本仓库根目录的 `.gitignore` 就是这个模板生成的，里面有：

```
[Bb]in/
[Oo]bj/
```

⚠️ 如果 `bin/`、`obj/` 已经被提交过，加 `.gitignore` 不会自动删掉，要 `git rm -r --cached bin obj`。

## 9. 编译警告：CS、CA、IDE 🟡

编译输出里的警告编号前缀代表不同来源：

| 前缀 | 来源 | 例子 | 编译时默认报吗 |
|---|---|---|---|
| `CSxxxx` | C# 编译器 | `CS8602` 可能解引用 null、`CS0168` 变量未使用 | ✅ |
| `CAxxxx` | 代码质量分析器（.NET SDK 自带） | `CA1822` 成员可以标为 static | 少数默认报，大多只是 IDE 里的建议 |
| `IDExxxx` | 代码风格分析器 | `IDE0161` 用文件范围命名空间、`IDE0005` 多余 using | ❌ 默认只在 IDE 里提示 |
| `SYSLIBxxxx` | 过时或不推荐的 API | `SYSLIB0014` WebRequest 已过时 | ✅ |
| `ILxxxx` | 裁剪 / AOT 分析 | `IL2026`、`IL3050` | 开 AOT / 裁剪时报 |
| `NUxxxx` | NuGet | `NU1900` 拿不到漏洞数据 | ✅ |
| `xUnitxxxx` | xUnit 自带的分析器 | `xUnit2013` | ✅（测试项目里） |

### 9.1 用 .editorconfig 配置严重级别

在项目（或解决方案、仓库）根目录放一个 `.editorconfig`，对它所在目录及子目录生效。也可以用 `dotnet new editorconfig` 生成一份完整的默认配置。

实测用的代码：

```csharp
namespace Demo
{
    internal class Calc
    {
        public int Add(int a, int b) => a + b;   // 没用到实例成员 → CA1822 建议改成 static
    }

    internal class Program
    {
        static void Main()
        {
            string? s = Console.ReadLine();
            Console.WriteLine(s.Length);          // CS8602
            Console.WriteLine(new Calc().Add(1, 2));
        }
    }
}
```

没有 `.editorconfig` 时 `dotnet build`：

```
Program.cs(13,31): warning CS8602: Dereference of a possibly null reference.
```

加上 `.editorconfig`：

```ini
root = true

[*.cs]
# 编译器警告升级为错误
dotnet_diagnostic.CS8602.severity = error
# 代码质量分析（CA）：默认只是建议，改成警告
dotnet_diagnostic.CA1822.severity = warning
# 代码风格（IDE）：要求文件范围命名空间
csharp_style_namespace_declarations = file_scoped
dotnet_diagnostic.IDE0161.severity = warning
```

```
Program.cs(13,31): error CS8602: Dereference of a possibly null reference.
Program.cs(5,20): warning CA1822: Member 'Add' does not access instance data and can be marked as static (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1822)
```

`IDE0161` 还是没出现：**IDE 规则在命令行编译时默认不检查**，要在 csproj 里加 `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`（实测用 `-p:EnforceCodeStyleInBuild=true`）：

```
Program.cs(1,1): warning IDE0161: Convert to file-scoped namespace (https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0161)
Program.cs(13,31): error CS8602: Dereference of a possibly null reference.
Program.cs(5,20): warning CA1822: Member 'Add' does not access instance data and can be marked as static (...)
```

| 严重级别 | 效果 |
|---|---|
| `error` | 编译失败 |
| `warning` | 编译警告 |
| `suggestion` | 只在 IDE 里显示省略号提示 |
| `silent` | 只提供快速修复，不提示 |
| `none` | 完全关掉 |

### 9.2 其他配置方式

| 方式 | 写法 | 范围 |
|---|---|---|
| 整个项目所有警告当错误 | csproj 里 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` | 项目 |
| 关掉某些警告 | csproj 里 `<NoWarn>$(NoWarn);CS1591</NoWarn>` | 项目 |
| 只在一段代码里关 | `#pragma warning disable SYSLIB0014` … `#pragma warning restore SYSLIB0014` | 文件中的一段 |
| 提高 CA 规则的整体级别 | `<AnalysisLevel>latest-recommended</AnalysisLevel>` | 项目 |

建议：新项目开 `Nullable`，把 `CS8602` 这类可空警告认真处理掉，而不是用 `#pragma` 一关了之。

## 10. 小结

1. `dotnet new / build / run / test / add package / add reference / sln` 覆盖了 Maven 的日常用法；.NET 10 起还有 `dotnet package add` 这类名词在前的写法。
2. 单文件练习用 file-based app，配置写在 `#:package`、`#:property` 里；多文件、要测试、要部署就建项目，可以用 `dotnet project convert` 转。file-based app 默认 `PublishAot=true`。
3. csproj 记住四个属性：`OutputType`、`TargetFramework`、`ImplicitUsings`、`Nullable`；`System.Text` 等不在隐式 using 里。
4. 解决方案（`.slnx`）组织多个项目，按 App / Core / Tests 分层，依赖单向。
5. `internal` 的边界是程序集（项目），不是命名空间；测试要访问就用 `InternalsVisibleTo`。
6. NuGet 依赖写成 `PackageReference`，缓存在 `~/.nuget/packages/`；`Version` 是最低版本。
7. xUnit：`[Fact]` 单个用例，`[Theory]` + `[InlineData]` 多组数据；`Assert.Equal` 先写期望值；`Assert.Throws<T>` 要求类型完全一致。
8. 默认是 Debug，部署和多线程测试要用 `-c Release`；`bin/`、`obj/` 不提交；警告的严重级别用 `.editorconfig` 配，IDE 规则要 `EnforceCodeStyleInBuild` 才会在编译时报。

---

上一章：[线程同步 🟡](14-synchronization.md) · [目录](../README.md) · 下一章：[常见坑与下一步 🟢](16-pitfalls-and-next.md)
