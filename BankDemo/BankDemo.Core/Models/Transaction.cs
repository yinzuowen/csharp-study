namespace BankDemo.Core.Models;

/// <summary>
/// 一笔交易。
/// record：专门用来存数据的类型，自动生成构造函数、属性、Equals、ToString。
/// 金额用 decimal 而不是 double，避免浮点误差。
/// </summary>
public record Transaction(TransactionType Type, decimal Amount, string Description);
