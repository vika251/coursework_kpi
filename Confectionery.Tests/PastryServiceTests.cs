using Xunit;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using FluentValidation;
using FluentValidation.Results;
using ConfectioneryApi.Services;
using ConfectioneryApi.Repositories;
using ConfectioneryApi.Models;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Validators;
using ConfectioneryApi.Data;

namespace Confectionery.Tests
{
    public class PastryServiceTests
    {
        // Мок-об'єкти (імітації залежностей)
        private readonly Mock<IRepository<Pastry>> _mockRepo;
        private readonly IMemoryCache _cache; // Використовуємо реальний кеш в пам'яті для тестів
        private readonly Mock<DeletePastryValidator> _mockValidator;
        
        // System Under Test (SUT) - сервіс, який ми тестуємо
        private readonly PastryService _service;

        public PastryServiceTests()
        {
            // ARRANGE (Загальне налаштування для всіх тестів)
            _mockRepo = new Mock<IRepository<Pastry>>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            // Мокаємо валідатор. Передаємо null у конструктор, бо реальна логіка не буде викликатися.
            _mockValidator = new Mock<DeletePastryValidator>((ConfectioneryDbContext)null!);

            _service = new PastryService(_mockRepo.Object, _cache, _mockValidator.Object);
        }

        // ГРУПА ТЕСТІВ: GET ALL 

        // Тест 1: Перевірка отримання даних з БД та запису в кеш
        // Використовує: Assert.True, Assert.Equal, Verify(Times.Once)
        [Fact]
        public async Task GetAllPastriesAsync_ShouldLoadFromDb_AndSetCache_WhenCacheIsEmpty()
        {
            // ARRANGE
            var pastriesFromDb = new List<Pastry> { new Pastry { Id = 1, Name = "Cake", Price = 50 } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(pastriesFromDb);

            // ACT
            var result = await _service.GetAllPastriesAsync();

            // ASSERT
            Assert.True(result.IsSuccess); // Перевірка успішного виконання
            Assert.Single(result.Data!);   // Перевірка, що колекція містить 1 елемент
            
            // Verify: Переконуємося, що метод репозиторія був викликаний 1 раз
            _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
            
            // Перевірка, що дані потрапили в кеш
            Assert.True(_cache.TryGetValue("AllPastries", out _));
        }

        // Тест 2: Перевірка роботи кешу (Cache Hit)
        // Використовує: Assert.Equal, Verify(Times.Never) - перевірка, що в БД не ходили
        [Fact]
        public async Task GetAllPastriesAsync_ShouldReturnFromCache_WhenCacheExists()
        {
            // ARRANGE
            var cachedData = new List<PastryDto> { new PastryDto { Name = "Cached Item" } };
            _cache.Set("AllPastries", cachedData);

            // ACT
            var result = await _service.GetAllPastriesAsync();

            // ASSERT
            Assert.True(result.IsSuccess);
            Assert.Equal("Cached Item", result.Data!.First().Name); // Перевірка точного значення
            
            // Verify: Репозиторій НЕ повинен викликатися, бо дані взяли з кешу
            _mockRepo.Verify(r => r.GetAllAsync(), Times.Never);
        }

        // Тест 3: Перевірка порожньої колекції
        // Використовує: Assert.Empty
        [Fact]
        public async Task GetAllPastriesAsync_ShouldReturnEmptyList_WhenDbIsEmpty()
        {
            // ARRANGE
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Pastry>());

            // ACT
            var result = await _service.GetAllPastriesAsync();

            // ASSERT
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Data!); // Перевірка на порожнечу
        }

        // ГРУПА ТЕСТІВ: GET BY ID 

        // Тест 4: Успішний пошук
        // Використовує: Assert.NotNull
        [Fact]
        public async Task GetPastryByIdAsync_ShouldReturnSuccess_WhenFound()
        {
            // ARRANGE
            var pastry = new Pastry { Id = 1, Name = "Cake", Price = 50 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pastry);

            // ACT
            var result = await _service.GetPastryByIdAsync(1);

            // ASSERT
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data); // Перевірка на null
            Assert.Equal("Cake", result.Data!.Name);
        }

        // Тест 5: Пошук неіснуючого елемента
        // Використовує: Assert.False, Assert.Null, Assert.Equal (повідомлення про помилку)
        [Fact]
        public async Task GetPastryByIdAsync_ShouldReturnFailure_WhenNotFound()
        {
            // ARRANGE
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Pastry?)null);

            // ACT
            var result = await _service.GetPastryByIdAsync(99);

            // ASSERT
            Assert.False(result.IsSuccess); // Операція неуспішна
            Assert.Null(result.Data);       // Даних немає
            Assert.Equal("Виріб не знайдено", result.ErrorMessage); // Перевірка тексту помилки
        }

        // ГРУПА ТЕСТІВ: CREATE 

        // Тест 6: Створення та інвалідація кешу
        // Використовує: Assert.False (для перевірки відсутності ключа в кеші)
        [Fact]
        public async Task CreatePastryAsync_ShouldAddEntity_AndInvalidateCache()
        {
            // ARRANGE
            _cache.Set("AllPastries", "Old Data"); // "Забруднюємо" кеш
            var dto = new CreatePastryDto { Name = "New Cake", Price = 100 };

            // ACT
            var result = await _service.CreatePastryAsync(dto);

            // ASSERT
            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Pastry>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            
            // Перевіряємо, що ключ видалено з кешу
            Assert.False(_cache.TryGetValue("AllPastries", out _)); 
        }

        // Тест 7: Перевірка аргументів виклику репозиторія
        // Використовує: It.Is<T>(predicate)
        [Fact]
        public async Task CreatePastryAsync_ShouldCallAddWithCorrectData()
        {
            // ARRANGE
            var dto = new CreatePastryDto { Name = "Special Cake", Price = 99.99m };

            // ACT
            await _service.CreatePastryAsync(dto);

            // ASSERT & VERIFY
            // Перевіряємо, що в репозиторій передано об'єкт саме з таким ім'ям і ціною
            _mockRepo.Verify(r => r.AddAsync(It.Is<Pastry>(p => 
                p.Name == "Special Cake" && 
                p.Price == 99.99m
            )), Times.Once);
        }

        // ГРУПА ТЕСТІВ: UPDATE

        // Тест 8: Спроба оновлення неіснуючого запису
        [Fact]
        public async Task UpdatePastryAsync_ShouldReturnFailure_WhenNotFound()
        {
            // ARRANGE
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Pastry?)null);
            var dto = new UpdatePastryDto { Name = "Updated", Price = 200 };

            // ACT
            var result = await _service.UpdatePastryAsync(1, dto);

            // ASSERT
            Assert.False(result.IsSuccess);
            Assert.Equal("Виріб не знайдено", result.ErrorMessage);
        }

        // Тест 9: Параметризований тест оновлення
        // Використовує: [Theory], [InlineData]
        [Theory]
        [InlineData("Updated Name 1", 50)]
        [InlineData("Updated Name 2", 150.5)]
        public async Task UpdatePastryAsync_ShouldUpdateProperties_Correctly(string newName, decimal newPrice)
        {
            // ARRANGE
            var pastry = new Pastry { Id = 1, Name = "Old Name", Price = 10 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(pastry);
            
            var dto = new UpdatePastryDto { Name = newName, Price = newPrice };

            // ACT
            var result = await _service.UpdatePastryAsync(1, dto);

            // ASSERT
            Assert.True(result.IsSuccess);
            Assert.Equal(newName, pastry.Name);  // Перевіряємо, що об'єкт змінився
            Assert.Equal(newPrice, pastry.Price);
            
            _mockRepo.Verify(r => r.Update(pastry), Times.Once);
        }

        // ГРУПА ТЕСТІВ: DELETE

        // Тест 10: Валідація перед видаленням
        // Використовує: Assert.NotEqual (помилка не пуста)
        [Fact]
        public async Task DeletePastryAsync_ShouldFail_WhenValidationFails()
        {
            // ARRANGE
            var validationFailure = new ValidationResult(new[] { new ValidationFailure("Id", "Error") });
            
            // Налаштовуємо мок валідатора на повернення помилки
            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationFailure);

            // ACT
            var result = await _service.DeletePastryAsync(1);

            // ASSERT
            Assert.False(result.IsSuccess);
            Assert.NotEqual(string.Empty, result.ErrorMessage); // Помилка присутня
            
            // Verify: Видалення з БД не викликалося (Times.Never)
            _mockRepo.Verify(r => r.Delete(It.IsAny<Pastry>()), Times.Never);
        }

        // Тест 11: Видалення неіснуючого
        [Fact]
        public async Task DeletePastryAsync_ShouldFail_WhenNotFound()
        {
            // ARRANGE
            // Валідація проходить успішно
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<int>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult());
            
            // Але в базі нічого немає
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Pastry?)null);

            // ACT
            var result = await _service.DeletePastryAsync(1);

            // ASSERT
            Assert.False(result.IsSuccess);
            Assert.Equal("Виріб не знайдено", result.ErrorMessage);
        }

        // Тест 12: Успішне видалення
        // Використовує: Verify(Times.Once) для Delete і SaveChanges
        [Fact]
        public async Task DeletePastryAsync_ShouldSuccess_WhenValid()
        {
            // ARRANGE
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<int>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult());
            
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Pastry());
            _cache.Set("AllPastries", "Data");

            // ACT
            var result = await _service.DeletePastryAsync(1);

            // ASSERT
            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.Delete(It.IsAny<Pastry>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.False(_cache.TryGetValue("AllPastries", out _)); // Кеш очищено
        }

        // ГРУПА ТЕСТІВ: DELETE ALL 

        // Тест 13: Видалення всього
        [Fact]
        public async Task DeleteAllPastriesAsync_ShouldCallRepo_AndInvalidateCache()
        {
            // ARRANGE
            _cache.Set("AllPastries", "Data");

            // ACT
            var result = await _service.DeleteAllPastriesAsync();

            // ASSERT
            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.DeleteAllAsync(), Times.Once);
            Assert.False(_cache.TryGetValue("AllPastries", out _));
        }
    }
}