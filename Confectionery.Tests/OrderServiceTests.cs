using Xunit; // Головна бібліотека для тестів
using Moq;   // Бібліотека для створення імітацій (моків)
using Microsoft.Extensions.Options; // Для налаштувань (IOptions)
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

// Підключаємо простори імен з основного проєкту
using ConfectioneryApi.Services;
using ConfectioneryApi.Repositories;
using ConfectioneryApi.Models;
using ConfectioneryApi.Configuration;
using ConfectioneryApi.Dtos;

namespace Confectionery.Tests
{
    public class OrderServiceTests
    {
        // MOCK ОБ'ЄКТИ
        // Це "фейкові" залежності. Ми не використовуємо реальну БД.
        private readonly Mock<IRepository<Order>> _mockRepo;
        private readonly Mock<IOptions<OrderSettings>> _mockOptions;
        
        // Це реальний сервіс, який ми тестуємо
        private readonly OrderService _service;

        // Конструктор виконує роль етапу "ARRANGE" (Підготовка) для всіх тестів
        public OrderServiceTests()
        {
            // 1. Ініціалізуємо моки
            _mockRepo = new Mock<IRepository<Order>>();
            _mockOptions = new Mock<IOptions<OrderSettings>>();
            
            // 2. Налаштовуємо дефолтну поведінку конфігурації
            // Кажемо: "Коли сервіс спитає налаштування, поверни AllowNewOrders = true"
            _mockOptions.Setup(s => s.Value).Returns(new OrderSettings { AllowNewOrders = true });

            // 3. Створюємо екземпляр сервісу, передаючи йому наші фейкові залежності
            _service = new OrderService(_mockRepo.Object, _mockOptions.Object);
        }

        // ТЕСТ 1: Перевірка бізнес-правила "Заборона створення замовлень"
        [Fact]
        public async Task CreateOrder_ShouldFail_WhenConfigForbids()
        {
            // ARRANGE
            // 1. Змінюємо налаштування на "false"
            _mockOptions.Setup(s => s.Value).Returns(new OrderSettings { AllowNewOrders = false });

            // 2. ВАЖЛИВО: Створюємо НОВИЙ екземпляр сервісу тут.
            // Тому що глобальний '_service' був створений у конструкторі, коли налаштування були 'true'.
            var serviceWithRestriction = new OrderService(_mockRepo.Object, _mockOptions.Object);

            var dto = new CreateOrderDto 
            { 
                CustomerId = 1, 
                Items = new List<OrderItemDto> { new OrderItemDto { PastryId = 1, Quantity = 1 } } 
            };
            
            // ACT
            // Викликаємо саме наш НОВИЙ сервіс
            var result = await serviceWithRestriction.CreateOrderAsync(dto);
            
            // ASSERT
            Assert.False(result.IsSuccess); // Тепер це має пройти
            Assert.Equal("Створення нових замовлень тимчасово вимкнено.", result.ErrorMessage);
            
            // Перевіряємо, що репозиторій не викликався
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        }
        // ТЕСТ 2: Перевірка валідації вхідних даних (порожній список)
        [Fact]
        public async Task CreateOrder_ShouldThrowException_WhenItemsListIsEmpty()
        {
            // ARRANGE 
            var dto = new CreateOrderDto { Items = new List<OrderItemDto>() }; // Порожній список

            // ACT & ASSERT 
            // Ми очікуємо, що сервіс викине виняток ArgumentException.
            // Це перевіряє надійність коду.
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateOrderAsync(dto));
            
            Assert.Equal("Замовлення не може бути порожнім", exception.Message);
        }

        // ТЕСТ 3: Успішне створення замовлення
        [Fact]
        public async Task CreateOrder_ShouldReturnData_AndCallSave_WhenValid()
        {
            // ARRANGE
            // Налаштування за замовчуванням вже дозволяють створення (з конструктора)
            var dto = new CreateOrderDto 
            { 
                CustomerId = 10,
                Items = new List<OrderItemDto> { new OrderItemDto { PastryId = 5, Quantity = 2 } } 
            };

            // ACT 
            var result = await _service.CreateOrderAsync(dto);

            // ASSERT
            // 1. Перевіряємо успіх
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(10, result.Data!.CustomerId); // Перевіряємо, що ID клієнта зберігся

            // 2. Verify: Перевіряємо, що метод AddAsync БУВ викликаний рівно 1 раз.
            // Ми також перевіряємо, що об'єкт, який передали в БД, має правильний статус.
            _mockRepo.Verify(r => r.AddAsync(It.Is<Order>(o => o.Status == OrderStatus.Нове)), Times.Once);
            
            // 3. Перевіряємо, що зміни були зафіксовані (Commit транзакції)
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // ТЕСТ 4: Перевірка бізнес-логіки статусу (Theory - параметризований тест)
        // Якщо кількість <= 10 -> "Нове", якщо > 10 -> "Обробляється"
        [Theory]
        [InlineData(5, OrderStatus.Нове)]           // Сценарій 1: мало товарів
        [InlineData(50, OrderStatus.Обробляється)]  // Сценарій 2: багато товарів
        public async Task CreateOrder_ShouldSetCorrectStatus_BasedOnQuantity(int quantity, OrderStatus expectedStatus)
        {
            // ARRANGE 
            var dto = new CreateOrderDto 
            { 
                Items = new List<OrderItemDto> { new OrderItemDto { Quantity = quantity } } 
            };

            // ACT
            var result = await _service.CreateOrderAsync(dto);

            // ASSERT
            Assert.True(result.IsSuccess);
            // Перевіряємо, що сервіс автоматично виставив правильний статус
            Assert.Equal(expectedStatus.ToString(), result.Data!.Status);
        }
    }
}