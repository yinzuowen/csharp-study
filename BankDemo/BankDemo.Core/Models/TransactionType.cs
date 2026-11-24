namespace BankDemo.Core.Models;

/// <summary>交易类型（enum：一组命名常量）</summary>
public enum TransactionType
{
    Deposit,     // 存款
    Withdrawal,  // 取款
    Invalid      // 无法识别
}
