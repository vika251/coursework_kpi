using Xunit;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using FluentValidation;
using FluentValidation.Results;

// Підключаємо простори імен нашого проєкту
using ConfectioneryApi.Services;
using ConfectioneryApi.Repositories;
using ConfectioneryApi.Models;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Validators;
using ConfectioneryApi.Data;

namespace Confectionery.Tests
{
    public class CustomerServiceTests
    {
        // ARRANGE (Загальна підготовка) 
        
        private readonly Mock<IRepository<Customer>> _mockRepo;
        // Мокаємо конкретний клас валідатора (передаємо null у конструктор, бо логіка буде підмінена)
        private readonly Mock<DeleteCustomerValidator> _mockValidator;
        private readonly CustomerService _service;

        public CustomerServiceTests()
        {
            _mockRepo = new Mock<IRepository<Customer>>();
            _mockValidator = new Mock<DeleteCustomerValidator>((ConfectioneryDbContext)null!);
            
            // Створюємо екземпляр сервісу з фейковими залежностями
            _service = new CustomerService(_mockRepo.Object, _mockValidator.Object);
        }

        // GET ALL 

        // Тест 1: Перевірка, що повертається список DTO
        [Fact]
        public async Task GetAllCustomersAsync_ShouldReturnListOfMappedDtos()
        {
            // Arrange
            var customers = new List<Customer>
            {
                new Customer { Id = 1, Name = "Vika", Phone = "+380501111111" },
                new Customer { Id = 2, Name = "Ivan", Phone = "+380502222222" }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(customers);

            // Act
            var result = await _service.GetAllCustomersAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "Vika");
            Assert.Contains(result, c => c.Name == "Ivan");
        }

        // Тест 2: Перевірка порожнього списку
        [Fact]
        public async Task GetAllCustomersAsync_ShouldReturnEmpty_WhenRepoIsEmpty()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Customer>());

            // Act
            var result = await _service.GetAllCustomersAsync();

            // Assert
            Assert.Empty(result);
        }

        // GET BY ID 

        // Тест 3: Успішний пошук
        [Fact]
        public async Task GetCustomerByIdAsync_ShouldReturnSuccess_WhenFound()
        {
            // Arrange
            var customer = new Customer { Id = 1, Name = "Test User", Phone = "123" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

            // Act
            var result = await _service.GetCustomerByIdAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Test User", result.Data!.Name);
            Assert.Equal("123", result.Data.Phone);
        }

        // Тест 4: Клієнта не знайдено
        [Fact]
        public async Task GetCustomerByIdAsync_ShouldReturnFailure_WhenNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Customer?)null);

            // Act
            var result = await _service.GetCustomerByIdAsync(99);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Клієнта не знайдено", result.ErrorMessage);
        }

        // CREATE 

        // Тест 5: Створення клієнта (перевірка виклику репозиторія)
        [Fact]
        public async Task CreateCustomerAsync_ShouldCallAddAndSaveChanges()
        {
            // Arrange
            var dto = new CreateCustomerDto { Name = "New User", Phone = "+380000000000" };

            // Act
            var result = await _service.CreateCustomerAsync(dto);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("New User", result.Data!.Name);

            // Verify: Перевіряємо, що метод AddAsync був викликаний з правильними даними
            _mockRepo.Verify(r => r.AddAsync(It.Is<Customer>(c => 
                c.Name == "New User" && 
                c.Phone == "+380000000000"
            )), Times.Once);
            
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // UPDATE 

        // Тест 6: Оновлення неіснуючого клієнта
        [Fact]
        public async Task UpdateCustomerAsync_ShouldReturnFailure_WhenNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Customer?)null);
            var dto = new UpdateCustomerDto { Name = "Updated", Phone = "111" };

            // Act
            var result = await _service.UpdateCustomerAsync(1, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Клієнта не знайдено", result.ErrorMessage);
            _mockRepo.Verify(r => r.Update(It.IsAny<Customer>()), Times.Never);
        }

        // Тест 7: Успішне оновлення (Parametrized Test)
        [Theory]
        [InlineData("Updated Name", "+380123456789")]
        [InlineData("Another Name", "+380987654321")]
        public async Task UpdateCustomerAsync_ShouldUpdateFields_WhenFound(string newName, string newPhone)
        {
            // Arrange
            var customer = new Customer { Id = 1, Name = "Old", Phone = "000" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
            
            var dto = new UpdateCustomerDto { Name = newName, Phone = newPhone };

            // Act
            var result = await _service.UpdateCustomerAsync(1, dto);

            // Assert
            Assert.True(result.IsSuccess);
            
            // Перевіряємо, що поля об'єкта дійсно змінилися
            Assert.Equal(newName, customer.Name);
            Assert.Equal(newPhone, customer.Phone);
            
            _mockRepo.Verify(r => r.Update(customer), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // DELETE

        // Тест 8: Видалення заблоковано валідатором (є активні замовлення)
        [Fact]
        public async Task DeleteCustomerAsync_ShouldFail_WhenValidationFails()
        {
            // Arrange
            var id = 1;
            // Налаштовуємо валідатор, щоб він повернув помилку
            var failure = new ValidationResult(new[] { new ValidationFailure("Id", "Є активні замовлення") });
            
            _mockValidator
                .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failure);

            // Act
            var result = await _service.DeleteCustomerAsync(id);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Є активні замовлення", result.ErrorMessage);
            
            // Репозиторій не повинен викликатися
            _mockRepo.Verify(r => r.Delete(It.IsAny<Customer>()), Times.Never);
        }

        // Тест 9: Клієнта не знайдено при видаленні
        [Fact]
        public async Task DeleteCustomerAsync_ShouldFail_WhenCustomerNotFound()
        {
            // Arrange
            // Валідація успішна
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<int>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult());
            
            // Але в базі немає
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Customer?)null);

            // Act
            var result = await _service.DeleteCustomerAsync(1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Клієнта не знайдено", result.ErrorMessage);
        }

        // Тест 10: Успішне видалення
        [Fact]
        public async Task DeleteCustomerAsync_ShouldSuccess_WhenValid()
        {
            // Arrange
            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<int>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult());
            
            var customer = new Customer { Id = 1 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

            // Act
            var result = await _service.DeleteCustomerAsync(1);

            // Assert
            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.Delete(customer), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        // DELETE ALL
        
        // Тест 11: Видалення всіх
        [Fact]
        public async Task DeleteAllCustomersAsync_ShouldCallRepoMethod()
        {
            // Act
            await _service.DeleteAllCustomersAsync();

            // Assert & Verify
            _mockRepo.Verify(r => r.DeleteAllAsync(), Times.Once);
        }
    }
}