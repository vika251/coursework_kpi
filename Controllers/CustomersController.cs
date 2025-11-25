using Microsoft.AspNetCore.Mvc;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Services; // Підключаємо сервіс
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConfectioneryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // -> /api/customers
    public class CustomersController : ControllerBase
    {
        // Замість репозиторія та валідатора тепер використовуємо один сервіс.
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // GET: /api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAllCustomers()
        {
            // Сервіс повертає вже готові DTO
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }

        // GET: /api/customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetCustomerById(int id)
        {
            var result = await _customerService.GetCustomerByIdAsync(id);
            
            // Якщо сервіс повернув помилку (наприклад, не знайдено)
            if (!result.IsSuccess) return NotFound();
            
            return Ok(result.Data);
        }

        // POST: /api/customers
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerDto createDto)
        {
            var result = await _customerService.CreateCustomerAsync(createDto);
            
            // Повертаємо 201 Created з посиланням на створений ресурс
            return CreatedAtAction(nameof(GetCustomerById), new { id = result.Data!.Id }, result.Data);
        }

        // PUT: /api/customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto updateDto)
        {
            var result = await _customerService.UpdateCustomerAsync(id, updateDto);
            
            if (!result.IsSuccess) return NotFound();
            
            return NoContent();
        }

        // DELETE: /api/customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var result = await _customerService.DeleteCustomerAsync(id);
            
            if (!result.IsSuccess)
            {
                // Розрізняємо помилки: 
                // Якщо клієнта немає - 404.
                // Якщо є активні замовлення (валідація) - 409 Conflict.
                if (result.ErrorMessage == "Клієнта не знайдено") return NotFound();
                return Conflict(result.ErrorMessage);
            }
            
            return NoContent();
        }

        // DELETE: /api/customers
        [HttpDelete]
        public async Task<IActionResult> DeleteAllCustomers()
        {
            await _customerService.DeleteAllCustomersAsync();
            return Ok(new { message = "All customers have been deleted." });
        }
    }
}