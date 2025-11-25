using Microsoft.AspNetCore.Mvc;
using ConfectioneryApi.Models;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using ConfectioneryApi.Configuration;  
using Microsoft.Extensions.Options;

namespace ConfectioneryApi.Controllers
{
    // Атрибут, що вказує, що це контролер для API. Вмикає автоматичну обробку помилок валідації.
    [ApiController]
    // Визначає базовий маршрут для всіх методів у цьому контролері.
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        // Приватне поле для зберігання репозиторія замовлень.
        private readonly IRepository<Order> _orderRepository;

        // 3. Приватне поле для зберігання наших налаштувань.
        // Воно буде містити дані з appsettings.json (AllowNewOrders).
        private readonly OrderSettings _orderSettings;

        // 4. Оновлюємо конструктор контролера.
        // Окрім репозиторія, ми тепер запитуємо IOptions<OrderSettings> через ін'єкцію залежностей.
        // IoC-контейнер надасть нам цей об'єкт, оскільки ми зареєстрували його в Program.cs.
        public OrdersController(IRepository<Order> orderRepository, IOptions<OrderSettings> orderSettings)
        {
            _orderRepository = orderRepository;
        // 5. Отримуємо сам об'єкт налаштувань з "обгортки" IOptions
            _orderSettings = orderSettings.Value;
        }

        // GET: /api/orders
        // Асинхронний метод для отримання списку всіх замовлень.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
        {
            var orders = await _orderRepository.GetAllAsync();
            
            // Мапимо замовлення в DTO.
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                // Конвертуємо enum OrderStatus у рядок для відповіді API.
                Status = o.Status.ToString(),
                OrderTime = o.OrderTime
            });
            
            return Ok(orderDtos);
        }

        // GET: /api/orders/5
        // Асинхронний метод для отримання одного замовлення за його ID.
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDetailsDto>> GetOrderById(int id)
        {
            // Завантажуємо замовлення і ОДРАЗУ включаємо пов'язані з ним позиції (OrderItems).
            var order = await _orderRepository.GetByIdAsync(id, o => o.OrderItems);
            
            if (order == null)
            {
                return NotFound();
            }

            // Мапимо замовлення з уже завантаженими позиціями.
            var orderDetailsDto = new OrderDetailsDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                // Конвертуємо enum OrderStatus у рядок.
                Status = order.Status.ToString(),
                OrderTime = order.OrderTime,
                Items = order.OrderItems.Select(oi => new OrderItemDto 
                { 
                    PastryId = oi.PastryId, 
                    Quantity = oi.Quantity 
                }).ToList()
            };
            
            return Ok(orderDetailsDto);
        }

        // POST: /api/orders
        // Асинхронний метод для створення нового замовлення.
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createDto)
        {
            // 6. ПЕРЕВІРКА КОНФІГУРАЦІЇ
            // Звертаємось до налаштувань, які ми отримали з appsettings.json
            if (!_orderSettings.AllowNewOrders)
            {
                // Якщо в appsettings.json AllowNewOrders = false,
                // ми блокуємо створення замовлення і повертаємо користувачу помилку 503.
                return StatusCode(503, "Створення нових замовлень тимчасово вимкнено.");
            }


            // 7. Якщо перевірка пройдена (AllowNewOrders = true), 
            // виконується звичайний код створення замовлення:
            var newOrder = new Order
            {
                CustomerId = createDto.CustomerId,
                // Конвертуємо валідний рядок "Нове" в enum OrderStatus.Нове.
                Status = Enum.Parse<OrderStatus>(createDto.Status, true),
                OrderTime = DateTime.UtcNow,
                OrderItems = createDto.Items.Select(itemDto => new OrderItem
                {
                    PastryId = itemDto.PastryId,
                    Quantity = itemDto.Quantity
                }).ToList()
            };
            
            await _orderRepository.AddAsync(newOrder);
            // Зберігаємо все однією транзакцією.
            await _orderRepository.SaveChangesAsync();

            // Мапимо створене замовлення в DTO для відповіді.
            var orderDto = new OrderDto
            {
                Id = newOrder.Id,
                CustomerId = newOrder.CustomerId,
                Status = newOrder.Status.ToString(),
                OrderTime = newOrder.OrderTime
            };
            
            // Повертаємо відповідь 201 Created.
            return CreatedAtAction(nameof(GetOrderById), new { id = newOrder.Id }, orderDto);
        }

        // PUT: /api/orders/5
        // Асинхронний метод для оновлення існуючого замовлення.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, UpdateOrderDto updateDto)
        {
            // Для оновлення завантажуємо замовлення разом зі старими позиціями.
            var order = await _orderRepository.GetByIdAsync(id, o => o.OrderItems);
            if (order == null)
            {
                return NotFound();
            }

            // Безпечно конвертуємо рядок статусу з DTO в enum.
            if (!Enum.TryParse<OrderStatus>(updateDto.Status, true, out var newStatus))
            {
                // Ця помилка не має виникати, оскільки валідатор вже перевірив статус.
                return BadRequest("Вказано недійсний статус.");
            }

            // Перевіряємо, чи дозволений перехід між статусами.
            if (!IsStatusTransitionAllowed(order.Status, newStatus))
            {
                return Conflict($"Перехід зі статусу '{order.Status}' у '{newStatus}' заборонено.");
            }

            // Оновлюємо основні властивості замовлення.
            order.CustomerId = updateDto.CustomerId;
            order.Status = newStatus;

            // Видаляємо старі позиції.
            order.OrderItems.Clear();

            // Додаємо нові позиції.
            foreach (var itemDto in updateDto.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    PastryId = itemDto.PastryId,
                    Quantity = itemDto.Quantity
                });
            }

            _orderRepository.Update(order);
            // Зберігаємо всі зміни однією транзакцією.
            await _orderRepository.SaveChangesAsync();

            // Повертаємо 204 No Content.
            return NoContent();
        }

        // DELETE: /api/orders/5
        // Асинхронний метод для видалення замовлення.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _orderRepository.Delete(order);
            // Завдяки каскадному видаленню, позиції (OrderItems) видаляться автоматично.
            await _orderRepository.SaveChangesAsync();

            return NoContent();
        }
        
        // DELETE: /api/orders
        // Асинхронний метод для видалення всіх замовлень.
        [HttpDelete]
        public async Task<IActionResult> DeleteAllOrders()
        {
            await _orderRepository.DeleteAllAsync();
            return Ok(new { message = "All orders and their items have been deleted." });
        }

        //Приватний метод, що інкапсулює логіку переходів між статусами.
        private bool IsStatusTransitionAllowed(OrderStatus currentStatus, OrderStatus newStatus)
        {
            if (currentStatus == newStatus) return true;

            // Не можна змінювати фінальні статуси.
            if (currentStatus == OrderStatus.Виконано || currentStatus == OrderStatus.Скасовано)
            {
                return false;
            }
            
            // Правила переходів.
            switch (currentStatus)
            {
                case OrderStatus.Нове:
                    return newStatus == OrderStatus.Обробляється || newStatus == OrderStatus.Скасовано;
                
                case OrderStatus.Обробляється:
                    return newStatus == OrderStatus.Виконано || newStatus == OrderStatus.Скасовано;
                
                default:
                    return false;
            }
        }
    }
}