using FluentValidation;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Data; 
using Microsoft.EntityFrameworkCore;

namespace ConfectioneryApi.Validators
{
    public class OrderItemDtoValidator : AbstractValidator<OrderItemDto>
    {
        private readonly ConfectioneryDbContext _context;

        // Конструктор для отримання DbContext
        public OrderItemDtoValidator(ConfectioneryDbContext context)
        {
            _context = context;

            RuleFor(x => x.PastryId)
                .NotEmpty().WithMessage("ID виробу є обов'язковим.")
                // Додаємо перевірку на існування виробу
                .MustAsync(PastryExists).WithMessage("Виробу з таким ID не існує.");

            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, 100).WithMessage("Кількість повинна бути в діапазоні від 1 до 100.");
        }

        private async Task<bool> PastryExists(int pastryId, CancellationToken token)
        {
            return await _context.Pastries.AnyAsync(p => p.Id == pastryId, token);
        }
    }
}