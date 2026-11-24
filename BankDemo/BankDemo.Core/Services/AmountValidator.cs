namespace BankDemo.Core.Services;

/// <summary>
/// internal：只有 BankDemo.Core 项目内部能用。
/// BankDemo.App 看不到它；BankDemo.Tests 能看到，是因为 csproj 里配置了 InternalsVisibleTo。
/// </summary>
internal static class AmountValidator
{
    public static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "金额必须大于 0");
    }
}
