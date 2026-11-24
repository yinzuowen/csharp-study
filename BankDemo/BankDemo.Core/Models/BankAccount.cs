using BankDemo.Core.Services;

namespace BankDemo.Core.Models;

/// <summary>银行账户（class：引用类型）</summary>
public class BankAccount
{
    private readonly List<Transaction> _transactions = new();

    // 构造函数
    public BankAccount(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("户主不能为空", nameof(owner));
        Owner = owner;
    }

    // 只读自动属性：只能在构造函数里赋值
    public string Owner { get; }

    // 外部只读，类内部可写
    public decimal Balance { get; private set; }

    // 对外只暴露只读视图，外部无法直接 Add
    public IReadOnlyList<Transaction> Transactions => _transactions;

    public void Deposit(decimal amount, string description = "")
    {
        AmountValidator.EnsurePositive(amount);
        Balance += amount;
        _transactions.Add(new Transaction(TransactionType.Deposit, amount, description));
    }

    public void Withdraw(decimal amount, string description = "")
    {
        AmountValidator.EnsurePositive(amount);
        if (amount > Balance)
            throw new InvalidOperationException($"余额不足：当前 {Balance}，尝试取出 {amount}");
        Balance -= amount;
        _transactions.Add(new Transaction(TransactionType.Withdrawal, amount, description));
    }

    // 重写 ToString，Console.WriteLine(account) 时会用到
    public override string ToString() => $"{Owner} 的账户，余额 {Balance:N2}";
}
