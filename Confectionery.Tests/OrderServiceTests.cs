using Xunit; // Основний фреймворк тестування
using Moq;   // Бібліотека для імітації об'єктів (Mocking)
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

// Підключаємо простори імен нашого основного проєкту
using ConfectioneryApi.Services;
using ConfectioneryApi.Repositories;
using ConfectioneryApi.Models;
using ConfectioneryApi.Configuration;
using ConfectioneryApi.Dtos;

namespace Confectionery.Tests
{
    public class OrderServiceTests
    {
        // ARRANGE (Спільна підготовка для всіх тестів)
        
        // Мок репозиторія: імітує базу даних, щоб ми не залежали від реальної БД.
        private readonly Mock<IRepository<Order>> _mockRepo;
        
        // Мок налаштувань: імітує файл appsettings.json.
        private readonly Mock<IOptions<OrderSettings>> _mockOptions;
        
        // System Under Test (SUT): сам сервіс, який ми перевіряємо.
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _mockRepo = new Mock<IRepository<Order>>();
            _mockOptions = new Mock<IOptions<OrderSettings>>();
            
            // За замовчуванням налаштовуємо, що створення замовлень ДОЗВОЛЕНО (true).
            _mockOptions.Setup(s => s.Value).Returns(new OrderSettings { AllowNewOrders = true });

            // Ініціалізуємо сервіс із нашими фейковими залежностями.
            _service = new OrderService(_mockRepo.Object, _mockOptions.Object);
        }

        // ТЕСТ 1. Перевірка на NULL (Assert.Throws)
        // Сценарій: Якщо список товарів не ініціалізовано (null), має бути помилка.
        [Fact]
        public async Task CreateOrder_ShouldThrowException_WhenItemsIsNull()
        {
            // Arrange
            var dto = new CreateOrderDto { Items = null! }; // Передаємо null

            // Act & Assert
            // Перевіряємо, що метод викидає очікуваний виняток ArgumentException.
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateOrderAsync(dto));
            
            Assert.Equal("Замовлення не може бути порожнім", exception.Message);
        }

        // ТЕСТ 2. Перевірка на порожній список (Assert.Throws + Assert.Equal)
        // Сценарій: Список товарів є, але він порожній.
        [Fact]
        public async Task CreateOrder_ShouldThrowException_WhenItemsListIsEmpty()
        {
            // Arrange
            var dto = new CreateOrderDto { Items = new List<OrderItemDto>() };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateOrderAsync(dto));
            Assert.Equal("Замовлення не може бути порожнім", exception.Message);
        }

        // ТЕСТ 3. Перевірка конфігурації (Verify Times.Never + Assert.False)
        // Сценарій: У налаштуваннях заборонено створювати замовлення (AllowNewOrders = false).
        [Fact]
        public async Task CreateOrder_ShouldFail_WhenConfigForbids()
        {
            // Arrange
            // Переналаштовуємо мок, щоб зімітувати заборону.
            _mockOptions.Setup(s => s.Value).Returns(new OrderSettings { AllowNewOrders = false });
            
            // Важливо: створюємо новий екземпляр сервісу, щоб він підхопив нові налаштування.
            var serviceWithRestriction = new OrderService(_mockRepo.Object, _mockOptions.Object);

            var dto = new CreateOrderDto 
            { 
                Items = new List<OrderItemDto> { new OrderItemDto { Quantity = 1 } } 
            };

            // Act
            var result = await serviceWithRestriction.CreateOrderAsync(dto);

            // Assert
            Assert.False(result.IsSuccess); // Очікуємо Failure
            Assert.Equal("Створення нових замовлень тимчасово вимкнено.", result.ErrorMessage);

            // Verify: Перевіряємо, що метод збереження в БД НІКОЛИ не викликався.
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        }

        // ТЕСТ 4. Перевірка бізнес-правила (Assert.Contains + Assert.NotEqual)
        // Сценарій: Користувач намагається замовити занадто багато одиниць одного товару (>100).
        [Fact]
        public async Task CreateOrder_ShouldFail_WhenQuantityIsTooHigh()
        {
            // Arrange
            var dto = new CreateOrderDto 
            { 
                Items = new List<OrderItemDto> { new OrderItemDto { Quantity = 150 } } // 150 > 100
            };

            // Act
            var result = await _service.CreateOrderAsync(dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotEqual(string.Empty, result.ErrorMessage); // Повідомлення не має бути порожнім
            // Перевіряємо, що текст помилки містить ключові слова
            Assert.Contains("велика кількість", result.ErrorMessage); 
        }

        // ТЕСТ 5. Параметризований тест ([Theory] + [InlineData])
        // Сценарій: Перевірка автоматичного визначення статусу замовлення.
        // Правило: <= 10 шт -> "Нове", > 10 шт -> "Обробляється".
        [Theory]
        [InlineData(1, OrderStatus.Нове)]           // Кейс А: 1 шт
        [InlineData(10, OrderStatus.Нове)]          // Кейс Б: 10 шт (граничне значення)
        [InlineData(11, OrderStatus.Обробляється)]  // Кейс В: 11 шт
        [InlineData(50, OrderStatus.Обробляється)]  // Кейс Г: 50 шт
        public async Task CreateOrder_ShouldSetCorrectStatus_BasedOnQuantity(int quantity, OrderStatus expectedStatus)
        {
            // Arrange
            var dto = new CreateOrderDto 
            { 
                CustomerId = 123,
                Items = new List<OrderItemDto> { new OrderItemDto { PastryId = 1, Quantity = quantity } } 
            };

            // Act
            var result = await _service.CreateOrderAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            // Перевіряємо, що статус у відповіді співпадає з очікуваним
            Assert.Equal(expectedStatus.ToString(), result.Data!.Status);
        }

        // ТЕСТ 6. Перевірка аргументів виклику (It.Is + Verify)
        // Сценарій: Перевіряємо, що в базу даних передається правильний об'єкт Order.
        [Fact]
        public async Task CreateOrder_ShouldCallRepository_WithCorrectData()
        {
            // Arrange
            var dto = new CreateOrderDto 
            { 
                CustomerId = 5,
                Items = new List<OrderItemDto> { new OrderItemDto { PastryId = 2, Quantity = 3 } } 
            };

            // Act
            await _service.CreateOrderAsync(dto);

            // Assert & Verify
            // Ми перевіряємо, що метод AddAsync був викликаний з об'єктом Order,
            // у якого CustomerId == 5 і кількість позицій == 1.
            _mockRepo.Verify(r => r.AddAsync(It.Is<Order>(o => 
                o.CustomerId == 5 && 
                o.OrderItems.Count == 1
            )), Times.Once);
        }

        // ТЕСТ 7. Перевірка успішного результату (Assert.NotNull + Verify Times.Exactly)
        // Сценарій: Повний успішний цикл створення.
        [Fact]
        public async Task CreateOrder_ShouldReturnSuccess_AndSaveChanges()
        {
            // Arrange
            var dto = new CreateOrderDto 
            { 
                Items = new List<OrderItemDto> { new OrderItemDto { Quantity = 2 } } 
            };

            // Act
            var result = await _service.CreateOrderAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data); // Дані не повинні бути null
            Assert.Null(result.ErrorMessage); // Помилки не повинно бути

            // Перевіряємо, що транзакція була зафіксована (SaveChangesAsync викликано 1 раз)
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Exactly(1));
        }
    }
}