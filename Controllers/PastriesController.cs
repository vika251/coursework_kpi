using Microsoft.AspNetCore.Mvc;
using ConfectioneryApi.Models;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using ConfectioneryApi.Validators;
using FluentValidation.Results;

// 1. Підключаємо простір імен для роботи з кешем у пам'яті
using Microsoft.Extensions.Caching.Memory;

namespace ConfectioneryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // -> /api/pastries
    public class PastriesController : ControllerBase
    {
        // Впровадження залежності репозиторія для виробів.
        private readonly IRepository<Pastry> _pastryRepository;
        // Приватне поле для валідатора видалення.
        private readonly DeletePastryValidator _deleteValidator;

        //  Поля для кешування 
        // Приватне поле для зберігання сервісу кешування.
        // Ми отримаємо його через ін'єкцію залежностей у конструкторі.
        private readonly IMemoryCache _cache;
        
        // Визначаємо константу для ключа кешу.
        // Це допомагає уникнути помилок при написанні рядка "AllPastries" у різних місцях.
        private const string PastriesCacheKey = "AllPastries";

        // 2. Оновлюємо конструктор:
        // Додаємо IMemoryCache cache як параметр.
        // IoC-контейнер автоматично надасть нам цей сервіс,
        // оскільки ми зареєстрували його в Program.cs (AddMemoryCache()).
        public PastriesController(IRepository<Pastry> pastryRepository, 
                                  DeletePastryValidator deleteValidator,
                                  IMemoryCache cache) 
        {
            _pastryRepository = pastryRepository;
            _deleteValidator = deleteValidator;
            _cache = cache; // 3. Зберігаємо отриманий сервіс у приватне поле
        }

        // GET: /api/pastries
        // Отримати всі кондитерські вироби.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PastryDto>>> GetAllPastries()
        {
            // Логіка читання з кешу 

            // 4. Спочатку перевіряємо, чи є дані у кеші за нашим ключем.
            if (_cache.TryGetValue(PastriesCacheKey, out IEnumerable<PastryDto> pastryDtos))
            {
                // 5. Якщо дані є в кеші (TryGetValue повернув true),
                // миттєво повертаємо їх, не звертаючись до бази даних.
                // Це прискорює відповідь і зменшує навантаження на БД.
                return Ok(pastryDtos);
            }
            // Логіка отримання даних з бази даних, якщо в кеші немає

            // 6. Якщо в кеші даних немає (cache miss), ми виконуємо звичайний код:
            // йдемо до репозиторія і отримуємо дані з бази даних.
            var pastries = await _pastryRepository.GetAllAsync();
            pastryDtos = pastries.Select(p => new PastryDto 
            { 
                Id = p.Id, 
                Name = p.Name, 
                Price = p.Price 
            });

            //  Логіка запису в кеш 

            // 7. Налаштовуємо опції для збереження даних у кеш.
            var cacheOptions = new MemoryCacheEntryOptions()
                // Встановлюємо абсолютний час "життя" кешу (наприклад, 5 хвилин).
                // Після 5 хвилин ці дані будуть автоматично видалені з кешу.
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

           // 8. Зберігаємо отримані з БД дані в кеш за нашим ключем.
            _cache.Set(PastriesCacheKey, pastryDtos, cacheOptions);
            

            return Ok(pastryDtos); // Повертаємо дані, отримані з БД
        }

        // GET: /api/pastries/5
        // Отримати виріб за ID.
        [HttpGet("{id}")]
        public async Task<ActionResult<PastryDto>> GetPastryById(int id)
        {
            // (Цей метод не кешуємо, бо кешування за окремим ID
            // менш ефективне, ніж кешування повного списку)
            var pastry = await _pastryRepository.GetByIdAsync(id);
            if (pastry == null)
            {
                return NotFound();
            }
            var pastryDto = new PastryDto 
            { 
                Id = pastry.Id, 
                Name = pastry.Name, 
                Price = pastry.Price 
            };
            return Ok(pastryDto);
        }

        // POST: /api/pastries
        // Створити новий виріб.
        [HttpPost]
        public async Task<ActionResult<PastryDto>> CreatePastry(CreatePastryDto createDto)
        {
            var newPastry = new Pastry
            {
                Name = createDto.Name,
                Price = createDto.Price
            };
            
            await _pastryRepository.AddAsync(newPastry);
            await _pastryRepository.SaveChangesAsync();

            // ІНВАЛІДАЦІЯ КЕШУ 
            // 9. Оскільки ми додали новий виріб, кеш "AllPastries" застарів.
            // Ми видаляємо його, щоб уникнути показу неактуальних даних.
            _cache.Remove(PastriesCacheKey);

            var pastryDto = new PastryDto 
            { 
                Id = newPastry.Id, 
                Name = newPastry.Name, 
                Price = newPastry.Price 
            };
            
            return CreatedAtAction(nameof(GetPastryById), new { id = newPastry.Id }, pastryDto);
        }

        // PUT: /api/pastries/5
        // Оновити існуючий виріб.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePastry(int id, UpdatePastryDto updateDto)
        {
            var pastry = await _pastryRepository.GetByIdAsync(id);
            if (pastry == null)
            {
                return NotFound();
            }

            pastry.Name = updateDto.Name;
            pastry.Price = updateDto.Price;

            _pastryRepository.Update(pastry);
            await _pastryRepository.SaveChangesAsync();

            // ІНВАЛІДАЦІЯ КЕШУ 
            // 10. Ми оновили існуючий виріб. Кеш "AllPastries" більше не актуальний.
            _cache.Remove(PastriesCacheKey);

            return NoContent();
        }

        // DELETE: /api/pastries/5
        // Видалити виріб.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePastry(int id)
        {
            // Викликаємо валідатор вручну перед видаленням.
            var validationResult = await _deleteValidator.ValidateAsync(id);
            if (!validationResult.IsValid)
            {
                // Якщо виріб використовується в замовленнях, повертаємо помилку.
                return Conflict(validationResult.Errors.First().ErrorMessage);
            }

            var pastry = await _pastryRepository.GetByIdAsync(id);
            if (pastry == null)
            {
                return NotFound();
            }

            _pastryRepository.Delete(pastry);
            await _pastryRepository.SaveChangesAsync();
            
            // ІНВАЛІДАЦІЯ КЕШУ 
            // 11. Ми видалили виріб. Кеш "AllPastries" більше не актуальний.
            _cache.Remove(PastriesCacheKey);
            
            return NoContent();
        }

        // DELETE: /api/pastries
        // Метод для видалення всіх виробів.
        [HttpDelete]
        public async Task<IActionResult> DeleteAllPastries()
        {
            // цей метод видалить всі вироби без перевірки на використання в замовленнях.
            await _pastryRepository.DeleteAllAsync();
            
            // ІНВАЛІДАЦІЯ КЕШУ 
            // 12. Ми видалили всі вироби. Кеш "AllPastries" точно не актуальний.
            _cache.Remove(PastriesCacheKey);
            
            return Ok(new { message = "All pastries have been deleted." });
        }
    }
}