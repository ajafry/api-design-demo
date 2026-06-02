namespace ProductService.Domain.ValueObjects;

/// <summary>
/// Value object representing product pricing. Records provide structural equality and immutability out of the box.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "GBP")
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required.", nameof(currency));
        return new Money(amount, currency.ToUpperInvariant());
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
