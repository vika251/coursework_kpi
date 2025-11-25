using System.Threading.Tasks;
using ConfectioneryApi.Dtos;

namespace ConfectioneryApi.Services
{
    // Інтерфейс визначає методи, які повинен мати сервіс замовлень.
    // Це дозволяє використовувати Dependency Injection та Mock-об'єкти для тестів.
    public interface IOrderService
    {
        // Асинхронний метод створення замовлення.
        // Приймає DTO створення, повертає результат з DTO готового замовлення.
        Task<ServiceResult<OrderDto>> CreateOrderAsync(CreateOrderDto createDto);
    }
}