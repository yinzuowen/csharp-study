using BankDemo.Core.Models;
using BankDemo.Core.Parsing;
using BankDemo.Core.Services;

namespace BankDemo.Tests;

public class CsvTransactionParserTests
{
    private readonly CsvTransactionParser _parser = new();

    [Fact]
    public void Parse_ValidLines_ReturnsTransactions()
    {
        var result = _parser.Parse("""
            DEPOSIT, 100, salary
            withdrawal, 30, lunch
            """).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new Transaction(TransactionType.Deposit, 100m, "salary"), result[0]);  // record 按值比较
        Assert.Equal(TransactionType.Withdrawal, result[1].Type);  // 忽略大小写
    }

    [Theory]
    [InlineData("REFUND, 100, x")]   // 未知类型
    [InlineData("DEPOSIT, abc")]     // 金额不是数字
    [InlineData("DEPOSIT")]          // 缺少列
    public void Parse_BadLine_ReturnsInvalid(string line)
    {
        var result = _parser.Parse(line).Single();

        Assert.Equal(TransactionType.Invalid, result.Type);
    }

    [Fact]
    public void Parse_SkipsBlankLines()
    {
        var result = _parser.Parse("DEPOSIT, 1, a\n\n   \nDEPOSIT, 2, b");

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public void AmountValidator_IsVisibleToTests()
    {
        // AmountValidator 是 internal，因为 InternalsVisibleTo 所以测试项目能访问
        Assert.Throws<ArgumentOutOfRangeException>(() => AmountValidator.EnsurePositive(0));
    }
}
