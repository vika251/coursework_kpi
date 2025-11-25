using System;

namespace ConfectioneryApi.Dtos
{
    /// DTO для представлення короткої інформації про замовлення у відповідях API.
    public class OrderDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Status { get; set; } = string.Empty; // Ініціалізація
        public DateTime OrderTime { get; set; }
    }
}