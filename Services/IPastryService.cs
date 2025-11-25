using System.Collections.Generic;
using System.Threading.Tasks;
using ConfectioneryApi.Dtos;

namespace ConfectioneryApi.Services
{
    // Інтерфейс для сервісу роботи з кондитерськими виробами.
    // Він визначає контракт, який ми будемо використовувати в контролері.
    public interface IPastryService
    {
        // Повертає список усіх виробів (тут буде реалізовано кешування)
        Task<ServiceResult<IEnumerable<PastryDto>>> GetAllPastriesAsync();
        
        // Повертає один виріб за ID
        Task<ServiceResult<PastryDto>> GetPastryByIdAsync(int id);
        
        // Створює новий виріб
        Task<ServiceResult<PastryDto>> CreatePastryAsync(CreatePastryDto createDto);
        
        // Оновлює існуючий виріб
        Task<ServiceResult<bool>> UpdatePastryAsync(int id, UpdatePastryDto updateDto);
        
        // Видаляє виріб за ID
        Task<ServiceResult<bool>> DeletePastryAsync(int id);
        
        // Видаляє всі вироби
        Task<ServiceResult<bool>> DeleteAllPastriesAsync();
    }
}