using System.Collections.Generic;

namespace ConfectioneryApi.Dtos
{
    public class OrderItemDto
    {
        public int PastryId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }
}