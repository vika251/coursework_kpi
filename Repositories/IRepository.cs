using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ConfectioneryApi.Repositories
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();

        // Основний метод для отримання за ID, який може включати пов'язані сутності
        Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
        
        // Старий метод для зворотної сумісності
        Task<T?> GetByIdAsync(int id);
        
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
        Task DeleteAllAsync();
        Task<int> SaveChangesAsync();
    }
}