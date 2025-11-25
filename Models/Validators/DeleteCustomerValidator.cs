using FluentValidation;
using ConfectioneryApi.Data;
using ConfectioneryApi.Models; // Потрібно для OrderStatus
using Microsoft.EntityFrameworkCore;
using System.Threading;    
using System.Threading.Tasks;

namespace ConfectioneryApi.Validators
{
    public class DeleteCustomerValidator : AbstractValidator<int>
    {
        private readonly ConfectioneryDbContext _context;

        public DeleteCustomerValidator(ConfectioneryDbContext context)
        {
            _context = context;

            RuleFor(customerId => customerId)
                .MustAsync(NotHaveActiveOrders)
                .WithMessage("Неможливо видалити клієнта, оскільки у нього є активні замовлення.");
        }

        private async Task<bool> NotHaveActiveOrders(int customerId, CancellationToken cancellationToken)
        {
            bool hasActiveOrders = await _context.Orders.AnyAsync(o => 
                o.CustomerId == customerId && 
                o.Status != OrderStatus.Виконано && 
                o.Status != OrderStatus.Скасовано, 
                cancellationToken);

            return !hasActiveOrders;
        }
    }
}