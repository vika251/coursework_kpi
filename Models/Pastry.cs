using System.Collections.Generic; 
namespace ConfectioneryApi.Models
{
    public class Pastry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        
        // Навігаційна властивість
        // Це повідомляє Entity Framework, що один виріб (Pastry)
        // може бути пов'язаний з багатьма позиціями в замовленнях (OrderItems).
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}