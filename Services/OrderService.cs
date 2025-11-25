using System;
using System.Linq;
using System.Threading.Tasks;
using ConfectioneryApi.Configuration;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Models;
using ConfectioneryApi.Repositories;
using Microsoft.Extensions.Options;

namespace ConfectioneryApi.Services
{
    // Реалізація сервісу замовлень. Містить основну бізнес-логіку.
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly OrderSettings _settings;

        // Отримуємо залежності (Репозиторій та Налаштування) через конструктор.
        public OrderService(IRepository<Order> orderRepository, IOptions<OrderSettings> settings)
        {
            _orderRepository = orderRepository;
            _settings = settings.Value; // Отримуємо значення налаштувань
        }

        public async Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderDto createDto)
        {
            // 1. БІЗНЕС-ЛОГІКА: Перевірка конфігурації.
            // Якщо створення нових замовлень заборонено в налаштуваннях - повертаємо помилку.
            if (!_settings.AllowNewOrders)
            {
                return ServiceResult<OrderDto>.Failure("Створення нових замовлень тимчасово вимкнено.");
            }

            // 2. МАПІНГ: Перетворення CreateOrderDto в сутність Order.
            var newOrder = new Order
            {
                CustomerId = createDto.CustomerId,
                // Конвертуємо рядок статусу в Enum
                Status = Enum.Parse<OrderStatus>(createDto.Status, true),
                OrderTime = DateTime.UtcNow,
                // Створюємо список позицій замовлення
                OrderItems = createDto.Items.Select(itemDto => new OrderItem
                {
                    PastryId = itemDto.PastryId,
                    Quantity = itemDto.Quantity
                }).ToList()
            };

            // 3. ЗБЕРЕЖЕННЯ: Додаємо замовлення в БД через репозиторій.
            await _orderRepository.AddAsync(newOrder);
            await _orderRepository.SaveChangesAsync();

            // 4. МАПІНГ: Перетворення збереженої сутності Order назад в OrderDto для відповіді.
            var orderDto = new OrderDto
            {
                Id = newOrder.Id,
                CustomerId = newOrder.CustomerId,
                Status = newOrder.Status.ToString(),
                OrderTime = newOrder.OrderTime
            };

            // Повертаємо успішний результат з даними
            return ServiceResult<OrderDto>.Success(orderDto);
        }
    }
}