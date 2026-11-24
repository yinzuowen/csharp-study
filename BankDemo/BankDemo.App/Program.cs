using BankDemo.Core.Models;
using BankDemo.Core.Parsing;
using BankDemo.Core.Services;

// 顶级语句：这里就是程序入口（编译器会自动生成 Main）

string records = """
    DEPOSIT,   10000, Initial balance
    WITHDRAWAL, 1000, rent
    DEPOSIT,    2000, freelance payment
    WITHDRAWAL,  300, groceries
    REFUND,      100, unknown type
    DEPOSIT,     abc, bad amount
    WITHDRAWAL,  500, car maintenance
    """;

// 面向接口编程：变量类型是接口，具体实现可以随时替换
ITransactionParser parser = new CsvTransactionParser();
var account = new BankAccount("Owen");

foreach (var t in parser.Parse(records))
{
    try
    {
        switch (t.Type)
        {
            case TransactionType.Deposit:
                account.Deposit(t.Amount, t.Description);
                break;
            case TransactionType.Withdrawal:
                account.Withdraw(t.Amount, t.Description);
                break;
            default:
                Console.WriteLine($"跳过无效记录：{t.Description}");
                continue;
        }
        Console.WriteLine($"{t.Type,-10} {t.Amount,9:N2}  余额 {account.Balance,10:N2}  {t.Description}");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"失败：{ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine(account);   // 调用重写的 ToString()

// LINQ：对集合做筛选、汇总
decimal totalIn = account.Transactions
    .Where(t => t.Type == TransactionType.Deposit)
    .Sum(t => t.Amount);
decimal totalOut = account.Transactions
    .Where(t => t.Type == TransactionType.Withdrawal)
    .Sum(t => t.Amount);
Console.WriteLine($"总收入 {totalIn:N2}，总支出 {totalOut:N2}");

// 下面这行如果取消注释会编译失败：AmountValidator 是 internal，App 项目看不到
// AmountValidator.EnsurePositive(1);
