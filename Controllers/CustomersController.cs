using Microsoft.AspNetCore.Mvc;
using ConfectioneryApi.Models;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using ConfectioneryApi.Validators;
using FluentValidation.Results;

namespace ConfectioneryApi.Controllers
{
    // Атрибут, що вказує, що це контролер для API. Вмикає автоматичну обробку помилок валідації.
    [ApiController]
    // Визначає базовий маршрут для всіх методів у цьому контролері.
    [Route("api/[controller]")] // -> /api/customers
    public class CustomersController : ControllerBase
    {
        // Приватне поле для зберігання репозиторія клієнтів.
        private readonly IRepository<Customer> _customerRepository;
        // Приватне поле для валідатора видалення.
        private readonly DeleteCustomerValidator _deleteValidator;

        // Конструктор контролера, що приймає залежності через ін'єкцію (Dependency Injection).
        public CustomersController(IRepository<Customer> customerRepository, DeleteCustomerValidator deleteValidator)
        {
            _customerRepository = customerRepository;
            _deleteValidator = deleteValidator;
        }

        // GET: /api/customers
        // Асинхронний метод для отримання списку всіх клієнтів.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllCustomers()
        {
            // Отримуємо всі сутності клієнтів з бази даних.
            var customers = await _customerRepository.GetAllAsync();

            // Перетворюємо (мапимо) сутності бази даних (Customer) у DTO (CustomerDto).
            var customerDtos = customers.Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone
            });

            // Повертаємо DTO з кодом 200 OK.
            return Ok(customerDtos);
        }

        // GET: /api/customers/5
        // Асинхронний метод для отримання одного клієнта за його ID.
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
        {
            // Шукаємо клієнта в БД.
            var customer = await _customerRepository.GetByIdAsync(id);

            // Якщо клієнта не знайдено, повертаємо 404 Not Found.
            if (customer == null)
            {
                return NotFound();
            }

            // Мапимо знайдену сутність у DTO.
            var customerDto = new CustomerDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone
            };

            // Повертаємо DTO з кодом 200 OK.
            return Ok(customerDto);
        }

        // POST: /api/customers
        // Асинхронний метод для створення нового клієнта.
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerDto createDto)
        {
            // Створюємо нову сутність Customer на основі даних з DTO.
            var newCustomer = new Customer
            {
                Name = createDto.Name,
                Phone = createDto.Phone
            };

            // Додаємо нову сутність до контексту бази даних.
            await _customerRepository.AddAsync(newCustomer);
            // Зберігаємо зміни в БД.
            await _customerRepository.SaveChangesAsync();

            // Мапимо створену сутність (яка тепер має ID) у DTO для відповіді.
            var customerDto = new CustomerDto
            {
                Id = newCustomer.Id,
                Name = newCustomer.Name,
                Phone = newCustomer.Phone
            };

            // Повертаємо відповідь 201 Created.
            return CreatedAtAction(nameof(GetCustomerById), new { id = newCustomer.Id }, customerDto);
        }

        // PUT: /api/customers/5
        // Асинхронний метод для оновлення існуючого клієнта.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto updateDto)
        {
            // Шукаємо клієнта, якого потрібно оновити.
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound(); // Якщо не знайдено, повертаємо 404.
            }

            // Оновлюємо властивості сутності даними з DTO.
            customer.Name = updateDto.Name;
            customer.Phone = updateDto.Phone;

            // Позначаємо сутність як змінену.
            _customerRepository.Update(customer);
            // Зберігаємо зміни в БД.
            await _customerRepository.SaveChangesAsync();

            // Повертаємо 204 No Content.
            return NoContent();
        }

        // DELETE: /api/customers/5
        // Асинхронний метод для видалення клієнта.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            // Викликаємо валідатор вручну перед видаленням.
            var validationResult = await _deleteValidator.ValidateAsync(id);
            if (!validationResult.IsValid)
            {
                // Якщо валідація не пройдена (є активні замовлення), повертаємо помилку.
                return Conflict(validationResult.Errors.First().ErrorMessage);
            }

            // Шукаємо клієнта для видалення.
            var customer = await _customerRepository.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound(); // Якщо не знайдено, повертаємо 404.
            }

            // Видаляємо сутність.
            _customerRepository.Delete(customer);
            // Зберігаємо зміни.
            await _customerRepository.SaveChangesAsync();

            // Повертаємо 204 No Content.
            return NoContent();
        }

        // DELETE: /api/customers
        // Асинхронний метод для видалення всіх клієнтів.
        [HttpDelete]
        public async Task<IActionResult> DeleteAllCustomers()
        {
            // Цей метод видалить всіх клієнтів без перевірки на активні замовлення.
            await _customerRepository.DeleteAllAsync();
            return Ok(new { message = "All customers have been deleted." });
        }
    }
}