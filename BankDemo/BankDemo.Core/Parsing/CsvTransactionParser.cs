using BankDemo.Core.Models;
using BankDemo.Core.Services;

namespace BankDemo.Core.Parsing;

/// <summary>
/// 解析 "类型, 金额, 描述" 格式的文本。
/// 是之前 hello.cs 里 TransactionRecords 的改进版：
/// 跳过空行、列数不够不会崩溃、未知类型标为 Invalid。
/// </summary>
public class CsvTransactionParser : ITransactionParser
{
    public IEnumerable<Transaction> Parse(string text)
    {
        using var reader = new StringReader(text);   // using：用完自动释放
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(',', 3, StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || !decimal.TryParse(parts[1], out decimal amount))
            {
                yield return new Transaction(TransactionType.Invalid, 0m, line.Trim());
                continue;
            }

            string description = parts.Length > 2 ? parts[2] : "";

            // switch 表达式 + 忽略大小写的字符串匹配
            TransactionType type = parts[0].ToUpperInvariant() switch
            {
                "DEPOSIT" => TransactionType.Deposit,
                "WITHDRAWAL" => TransactionType.Withdrawal,
                _ => TransactionType.Invalid,
            };

            yield return new Transaction(type, amount, description);
        }
    }
}
