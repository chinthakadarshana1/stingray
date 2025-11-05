namespace Stingray.Domain;

public class Order
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
}