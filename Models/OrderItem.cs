namespace ConfectioneryApi.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        // Зовнішній ключ для замовлення
        public int OrderId { get; set; }
        
        // Зовнішній ключ для виробу
        public int PastryId { get; set; }

        // Навігаційні властивості
        // Повідомляє EF, що цей OrderItem належить одному конкретному замовленню (Order).
         public Order? Order { get; set; }

        // Повідомляє EF, що цей OrderItem пов'язаний з одним конкретним виробом (Pastry).
        public Pastry? Pastry { get; set; }
    }
}