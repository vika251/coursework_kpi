using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Models;
using ConfectioneryApi.Repositories;
using ConfectioneryApi.Validators; // Підключаємо валідатори
using Microsoft.Extensions.Caching.Memory; // Підключаємо кешування

namespace ConfectioneryApi.Services
{
    // Реалізація сервісу виробів.
    // Тут ми приховуємо всю логіку роботи з базою даних, кешем та валідацією.
    public class PastryService : IPastryService
    {
        private readonly IRepository<Pastry> _repository;
        private readonly IMemoryCache _cache;
        private readonly DeletePastryValidator _deleteValidator;
        
        // Ключ для збереження списку виробів у кеші
        private const string PastriesCacheKey = "AllPastries";

        // Отримуємо всі залежності через конструктор (Dependency Injection)
        public PastryService(IRepository<Pastry> repository, IMemoryCache cache, DeletePastryValidator deleteValidator)
        {
            _repository = repository;
            _cache = cache;
            _deleteValidator = deleteValidator;
        }

        public async Task<ServiceResult<IEnumerable<PastryDto>>> GetAllPastriesAsync()
        {
            // 1. ЛОГІКА КЕШУВАННЯ: Читання
            // Перевіряємо, чи є дані в кеші. Якщо є - повертаємо їх одразу.
            if (_cache.TryGetValue(PastriesCacheKey, out IEnumerable<PastryDto> pastryDtos))
            {
                return ServiceResult<IEnumerable<PastryDto>>.Success(pastryDtos);
            }

            // 2. РОБОТА З БД: Якщо в кеші пусто, йдемо в базу.
            var pastries = await _repository.GetAllAsync();
            
            // Мапимо (перетворюємо) сутності БД в DTO
            pastryDtos = pastries.Select(p => new PastryDto 
            { 
                Id = p.Id, 
                Name = p.Name, 
                Price = p.Price 
            });

            // 3. ЛОГІКА КЕШУВАННЯ: Запис
            // Зберігаємо отримані дані в кеш на 5 хвилин.
            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(PastriesCacheKey, pastryDtos, cacheOptions);

            return ServiceResult<IEnumerable<PastryDto>>.Success(pastryDtos);
        }

        public async Task<ServiceResult<PastryDto>> GetPastryByIdAsync(int id)
        {
            var pastry = await _repository.GetByIdAsync(id);
            
            if (pastry == null) 
            {
                return ServiceResult<PastryDto>.Failure("Виріб не знайдено");
            }

            var dto = new PastryDto { Id = pastry.Id, Name = pastry.Name, Price = pastry.Price };
            return ServiceResult<PastryDto>.Success(dto);
        }

        public async Task<ServiceResult<PastryDto>> CreatePastryAsync(CreatePastryDto createDto)
        {
            var newPastry = new Pastry { Name = createDto.Name, Price = createDto.Price };
            
            await _repository.AddAsync(newPastry);
            await _repository.SaveChangesAsync();

            // 4. ЛОГІКА КЕШУВАННЯ: Інвалідація
            // Дані змінилися, тому видаляємо старий кеш, щоб користувачі побачили новий список.
            _cache.Remove(PastriesCacheKey);

            var dto = new PastryDto { Id = newPastry.Id, Name = newPastry.Name, Price = newPastry.Price };
            return ServiceResult<PastryDto>.Success(dto);
        }

        public async Task<ServiceResult<bool>> UpdatePastryAsync(int id, UpdatePastryDto updateDto)
        {
            var pastry = await _repository.GetByIdAsync(id);
            if (pastry == null) return ServiceResult<bool>.Failure("Виріб не знайдено");

            pastry.Name = updateDto.Name;
            pastry.Price = updateDto.Price;

            _repository.Update(pastry);
            await _repository.SaveChangesAsync();

            // Інвалідація кешу після оновлення
            _cache.Remove(PastriesCacheKey);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DeletePastryAsync(int id)
        {
            // БІЗНЕС-ВАЛІДАЦІЯ: Перевіряємо, чи можна видаляти цей виріб
            // (наприклад, чи не використовується він у замовленнях)
            var validationResult = await _deleteValidator.ValidateAsync(id);
            if (!validationResult.IsValid)
            {
                // Якщо валідація не пройшла, повертаємо помилку з причиною
                return ServiceResult<bool>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            var pastry = await _repository.GetByIdAsync(id);
            if (pastry == null) return ServiceResult<bool>.Failure("Виріб не знайдено");

            _repository.Delete(pastry);
            await _repository.SaveChangesAsync();

            // Інвалідація кешу після видалення
            _cache.Remove(PastriesCacheKey);

            return ServiceResult<bool>.Success(true);
        }

        public async Task<ServiceResult<bool>> DeleteAllPastriesAsync()
        {
            await _repository.DeleteAllAsync();
            
            // Інвалідація кешу
            _cache.Remove(PastriesCacheKey);
            
            return ServiceResult<bool>.Success(true);
        }
    }
}