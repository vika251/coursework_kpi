using System;
using System.Collections.Generic;

namespace ConfectioneryApi.Dtos
{
    /// DTO для представлення повної інформації про замовлення, включаючи його позиції.
    public class OrderDetailsDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OrderTime { get; set; }
        
        // Тут ми можемо перевикористати OrderItemDto, який вже є
        public List<OrderItemDto> Items { get; set; } = new();
    }
}