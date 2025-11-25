using Microsoft.AspNetCore.Mvc;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Services; // Підключаємо наш новий сервіс
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ConfectioneryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PastriesController : ControllerBase
    {
        // Контролер тепер залежить ТІЛЬКИ від сервісу.
        // Він не знає ні про репозиторій, ні про кеш, ні про валідатор.
        private readonly IPastryService _pastryService;

        public PastriesController(IPastryService pastryService)
        {
            _pastryService = pastryService;
        }

        // GET: /api/pastries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PastryDto>>> GetAllPastries()
        {
            // Викликаємо метод сервісу, який сам розбереться з кешем і базою
            var result = await _pastryService.GetAllPastriesAsync();
            return Ok(result.Data);
        }

        // GET: /api/pastries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PastryDto>> GetPastryById(int id)
        {
            var result = await _pastryService.GetPastryByIdAsync(id);
            
            // Перевіряємо результат: якщо невдача - 404 Not Found
            if (!result.IsSuccess) return NotFound();
            
            return Ok(result.Data);
        }

        // POST: /api/pastries
        [HttpPost]
        public async Task<ActionResult<PastryDto>> CreatePastry(CreatePastryDto createDto)
        {
            var result = await _pastryService.CreatePastryAsync(createDto);
            return CreatedAtAction(nameof(GetPastryById), new { id = result.Data!.Id }, result.Data);
        }

        // PUT: /api/pastries/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePastry(int id, UpdatePastryDto updateDto)
        {
            var result = await _pastryService.UpdatePastryAsync(id, updateDto);
            if (!result.IsSuccess) return NotFound();
            return NoContent();
        }

        // DELETE: /api/pastries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePastry(int id)
        {
            var result = await _pastryService.DeletePastryAsync(id);
            
            if (!result.IsSuccess) 
            {
                // Розрізняємо помилки: "Не знайдено" (404) або "Бізнес-помилка" (409)
                if (result.ErrorMessage == "Виріб не знайдено") return NotFound();
                return Conflict(result.ErrorMessage);
            }
            
            return NoContent();
        }

        // DELETE: /api/pastries
        [HttpDelete]
        public async Task<IActionResult> DeleteAllPastries()
        {
            await _pastryService.DeleteAllPastriesAsync();
            return Ok(new { message = "All pastries have been deleted." });
        }
    }
}