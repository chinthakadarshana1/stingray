namespace Stingray.Application.DTOs;

public class CreateOrderRequest
{
    public Guid UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}