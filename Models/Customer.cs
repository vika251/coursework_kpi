using System.Collections.Generic;

namespace ConfectioneryApi.Models;

public class Customer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    // Навігаційна властивість для зв'язку з замовленнями
    public List<Order> Orders { get; set; } = new();
}