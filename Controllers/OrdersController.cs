using Microsoft.AspNetCore.Mvc;
using ConfectioneryApi.Models;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
// Підключаємо наші сервіси, щоб використовувати бізнес-логіку
using ConfectioneryApi.Services;

namespace ConfectioneryApi.Controllers
{
    // Атрибут, що вказує, що це API контролер
    [ApiController]
    [Route("api/[controller]")] // Шлях: /api/orders
    public class OrdersController : ControllerBase
    {
        // Репозиторій залишаємо, він потрібен для методів GET, PUT, DELETE
        private readonly IRepository<Order> _orderRepository;

        // Додаємо наш новий сервіс! 
        // Тепер він відповідає за логіку створення замовлень
        private readonly IOrderService _orderService;

        // Оновлений конструктор.
        // Приймаємо репозиторій ТА сервіс через ін'єкцію залежностей.
        // IOptions<OrderSettings> тут більше не потрібен, бо налаштування перевіряє сервіс.
        public OrdersController(IRepository<Order> orderRepository, IOrderService orderService)
        {
            _orderRepository = orderRepository;
            _orderService = orderService;
        }

        // GET: /api/orders
        // Отримати всі замовлення 
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
        {
            var orders = await _orderRepository.GetAllAsync();
            
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                CustomerId = o.CustomerId,
                Status = o.Status.ToString(),
                OrderTime = o.OrderTime
            });
            
            return Ok(orderDtos);
        }

        // GET: /api/orders/5
        // Отримати замовлення за ID 
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDetailsDto>> GetOrderById(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id, o => o.OrderItems);
            
            if (order == null)
            {
                return NotFound();
            }

            var orderDetailsDto = new OrderDetailsDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
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
        // Створення замовлення 
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto createDto)
        {
            // Ми більше не пишемо тут логіку перевірок і збереження.
            // Просто передаємо дані в сервіс, і він все робить сам.
            var result = await _orderService.CreateOrderAsync(createDto);

            // Перевіряємо результат, який повернув сервіс
            if (!result.IsSuccess)
            {
                // Якщо сталася помилка (наприклад, створення вимкнено в налаштуваннях)
                // Повертаємо помилку 503 (або можна 400) з повідомленням від сервісу.
                return StatusCode(503, result.ErrorMessage);
            }

            // Якщо успіх - повертаємо 201 Created
            // Готовий об'єкт замовлення (DTO) лежить у result.Data
            return CreatedAtAction(nameof(GetOrderById), new { id = result.Data!.Id }, result.Data);
        }

        // PUT: /api/orders/5
        // Оновлення замовлення (поки залишаємо стару реалізацію в контролері)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, UpdateOrderDto updateDto)
        {
            var order = await _orderRepository.GetByIdAsync(id, o => o.OrderItems);
            if (order == null)
            {
                return NotFound();
            }

            if (!Enum.TryParse<OrderStatus>(updateDto.Status, true, out var newStatus))
            {
                return BadRequest("Вказано недійсний статус.");
            }

            if (!IsStatusTransitionAllowed(order.Status, newStatus))
            {
                return Conflict($"Перехід зі статусу '{order.Status}' у '{newStatus}' заборонено.");
            }

            order.CustomerId = updateDto.CustomerId;
            order.Status = newStatus;

            order.OrderItems.Clear();

            foreach (var itemDto in updateDto.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    PastryId = itemDto.PastryId,
                    Quantity = itemDto.Quantity
                });
            }

            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: /api/orders/5
        // Видалення замовлення 
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _orderRepository.Delete(order);
            await _orderRepository.SaveChangesAsync();

            return NoContent();
        }
        
        // DELETE: /api/orders
        // Видалити всі замовлення 
        [HttpDelete]
        public async Task<IActionResult> DeleteAllOrders()
        {
            await _orderRepository.DeleteAllAsync();
            return Ok(new { message = "All orders and their items have been deleted." });
        }

        // Приватний метод перевірки статусів
        // Залишаємо тут, бо він використовується в UpdateOrder
        private bool IsStatusTransitionAllowed(OrderStatus currentStatus, OrderStatus newStatus)
        {
            if (currentStatus == newStatus) return true;

            if (currentStatus == OrderStatus.Виконано || currentStatus == OrderStatus.Скасовано)
            {
                return false;
            }
            
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