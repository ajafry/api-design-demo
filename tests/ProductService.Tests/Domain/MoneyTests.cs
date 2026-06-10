using ProductService.Domain.ValueObjects;

namespace ProductService.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsMoney()
    {
        var money = Money.Create(9.99m, "GBP");

        Assert.Equal(9.99m, money.Amount);
        Assert.Equal("GBP", money.Currency);
    }

    [Fact]
    public void Create_NormalisesToUpperCaseCurrency()
    {
        var money = Money.Create(1m, "gbp");

        Assert.Equal("GBP", money.Currency);
    }

    [Fact]
    public void Create_ZeroAmount_IsAllowed()
    {
        var money = Money.Create(0m, "GBP");

        Assert.Equal(0m, money.Amount);
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(-1m, "GBP"));
    }

    [Fact]
    public void Create_EmptyCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(1m, ""));
    }

    [Fact]
    public void Create_WhitespaceCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(1m, "   "));
    }

    [Fact]
    public void StructuralEquality_SameValues_AreEqual()
    {
        var a = Money.Create(10m, "GBP");
        var b = Money.Create(10m, "GBP");

        Assert.Equal(a, b);
    }

    [Fact]
    public void StructuralEquality_DifferentValues_AreNotEqual()
    {
        var a = Money.Create(10m, "GBP");
        var b = Money.Create(10m, "USD");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        var money = Money.Create(9.99m, "GBP");

        Assert.Equal("9.99 GBP", money.ToString());
    }
}
