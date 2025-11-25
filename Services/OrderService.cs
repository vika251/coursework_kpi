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
            // 1. БІЗНЕС-ЛОГІКА: Захист від некоректних даних
            // Перевіряємо, чи список позицій не порожній.
            // [Assert.Throws]: Цей випадок ми будемо ловити в тестах як виняток.
            if (createDto.Items == null || !createDto.Items.Any())
            {
                throw new ArgumentException("Замовлення не може бути порожнім");
            }

            // 2. БІЗНЕС-ЛОГІКА: Перевірка конфігурації.
            // [Verify(Times.Never)]: Якщо тут повернеться помилка, репозиторій не має викликатися.
            if (!_settings.AllowNewOrders)
            {
                return ServiceResult<OrderDto>.Failure("Створення нових замовлень тимчасово вимкнено.");
            }

            // 3. БІЗНЕС-ПРАВИЛО: Обмеження кількості.
            // [Assert.False]: Не можна замовляти більше 100 одиниць одного товару.
            if (createDto.Items.Any(i => i.Quantity > 100))
            {
                return ServiceResult<OrderDto>.Failure("Занадто велика кількість одного товару.");
            }

            // 4. БІЗНЕС-ЛОГІКА: Визначення початкового статусу.
            // [Theory]: Якщо товарів багато (> 10), замовлення відразу йде в "Обробляється".
            var totalQuantity = createDto.Items.Sum(i => i.Quantity);
            var initialStatus = totalQuantity > 10 ? OrderStatus.Обробляється : OrderStatus.Нове;

            // 5. МАПІНГ: Перетворення CreateOrderDto в сутність Order.
            var newOrder = new Order
            {
                CustomerId = createDto.CustomerId,
                Status = initialStatus, // Використовуємо обчислений статус
                OrderTime = DateTime.UtcNow,
                // Створюємо список позицій замовлення
                OrderItems = createDto.Items.Select(itemDto => new OrderItem
                {
                    PastryId = itemDto.PastryId,
                    Quantity = itemDto.Quantity
                }).ToList()
            };

            // 6. ЗБЕРЕЖЕННЯ: Додаємо замовлення в БД через репозиторій.
            // [Verify, It.Is]: Тест перевірить, що ми передали правильний об'єкт.
            await _orderRepository.AddAsync(newOrder);
            await _orderRepository.SaveChangesAsync();

            // 7. МАПІНГ: Перетворення збереженої сутності Order назад в OrderDto для відповіді.
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