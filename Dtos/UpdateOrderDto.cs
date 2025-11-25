using System.Collections.Generic;

namespace ConfectioneryApi.Dtos
{
    public class UpdateOrderDto
    {
        public int CustomerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
    }
}