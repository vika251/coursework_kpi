using System.Collections.Generic;

namespace ConfectioneryApi.Models
{
    public class Order
    {
        public int Id { get; set; }

        public DateTime OrderTime { get; set; }

        public OrderStatus Status { get; set; } 

        public int CustomerId { get; set; }

        // Навігаційна властивість для зв'язку з клієнтом
        public Customer? Customer { get; set; }
        
        // Навігаційна властивість для позицій замовлення
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}