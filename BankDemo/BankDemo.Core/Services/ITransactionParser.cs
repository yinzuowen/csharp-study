using BankDemo.Core.Models;

namespace BankDemo.Core.Services;

/// <summary>接口：只规定"能做什么"，不管"怎么做"</summary>
public interface ITransactionParser
{
    IEnumerable<Transaction> Parse(string text);
}
