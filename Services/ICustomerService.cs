using System.Collections.Generic;
using System.Threading.Tasks;
using ConfectioneryApi.Dtos;

namespace ConfectioneryApi.Services
{
    // Інтерфейс для сервісу роботи з клієнтами.
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerDto>> GetAllCustomersAsync();
        Task<ServiceResult<CustomerDto>> GetCustomerByIdAsync(int id);
        Task<ServiceResult<CustomerDto>> CreateCustomerAsync(CreateCustomerDto createDto);
        Task<ServiceResult<bool>> UpdateCustomerAsync(int id, UpdateCustomerDto updateDto);
        Task<ServiceResult<bool>> DeleteCustomerAsync(int id);
        Task DeleteAllCustomersAsync();
    }
}