using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ConfectioneryApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; 

namespace ConfectioneryApi.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        // Контекст бази даних (доступ до БД)
        private readonly ConfectioneryDbContext _context;
        // Набір даних (доступ до конкретної таблиці типу T)
        private readonly DbSet<T> _dbSet;
        // Сервіс для запису логів (журналу)
        private readonly ILogger<Repository<T>> _logger;

        // Конструктор. Отримує залежності (контекст і логгер) через ін'єкцію
        public Repository(ConfectioneryDbContext context, ILogger<Repository<T>> logger)
        {
            // Зберігаємо контекст БД
            _context = context;
            // Отримуємо конкретну таблицю (DbSet) для типу T
            _dbSet = context.Set<T>();
            // Зберігаємо логгер
            _logger = logger;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            // Логуємо початок операції отримання всіх сутностей
            _logger.LogInformation("Отримання всіх сутностей типу {EntityType}", typeof(T).Name);
            return await _dbSet.ToListAsync();
        }

        // Метод, що дозволяє завантажувати пов'язані дані (аналог JOIN).
        public async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            // Логуємо операцію пошуку за ID
            _logger.LogInformation("Отримання сутності типу {EntityType} за ID: {EntityId}", typeof(T).Name, id);
            IQueryable<T> query = _dbSet;

            // Додаємо до запиту всі вказані 'Include'.
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            // щоб знайти сутність за її первинним ключем "Id".
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        // Реалізація старого методу, яка викликає новий без пов'язаних даних.
        public async Task<T?> GetByIdAsync(int id)
        {
            // Логуємо операцію пошуку за ID (спрощений метод)
            _logger.LogInformation("Отримання сутності типу {EntityType} за ID: {EntityId} (Find)", typeof(T).Name, id);
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            // Логуємо операцію додавання нової сутності
            _logger.LogInformation("Додавання нової сутності типу {EntityType}", typeof(T).Name);
            await _dbSet.AddAsync(entity);
        }

        // Спрощена версія методу Update. 
        public void Update(T entity)
        {
            // Логуємо операцію оновлення сутності
            _logger.LogInformation("Оновлення сутності типу {EntityType}", typeof(T).Name);
            _dbSet.Update(entity);
        }

        // Спрощена версія методу Delete.
        public void Delete(T entity)
        {
            // Логуємо операцію видалення сутності
            _logger.LogInformation("Видалення сутності типу {EntityType}", typeof(T).Name);
            _dbSet.Remove(entity);
        }

        public async Task DeleteAllAsync()
        {
            // Логуємо операцію повного очищення таблиці
            _logger.LogInformation("Видалення ВСІХ сутностей типу {EntityType}", typeof(T).Name);
            await _dbSet.ExecuteDeleteAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            // Логуємо операцію збереження змін у базі даних
            _logger.LogInformation("Збереження змін у базі даних...");
            return await _context.SaveChangesAsync();
        }
    }
}