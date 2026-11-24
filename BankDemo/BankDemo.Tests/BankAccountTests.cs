using BankDemo.Core.Models;

namespace BankDemo.Tests;

public class BankAccountTests
{
    [Fact]   // [Fact]：一个测试用例，相当于 JUnit 的 @Test
    public void Deposit_IncreasesBalance()
    {
        var account = new BankAccount("Owen");

        account.Deposit(100m);

        Assert.Equal(100m, account.Balance);
        Assert.Single(account.Transactions);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_Throws()
    {
        var account = new BankAccount("Owen");
        account.Deposit(50m);

        Assert.Throws<InvalidOperationException>(() => account.Withdraw(100m));
        Assert.Equal(50m, account.Balance);   // 失败后余额不变
    }

    [Theory]   // [Theory] + [InlineData]：同一个测试用多组数据跑
    [InlineData(0)]
    [InlineData(-10)]
    public void Deposit_NonPositiveAmount_Throws(decimal amount)
    {
        var account = new BankAccount("Owen");

        Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount));
    }
}
