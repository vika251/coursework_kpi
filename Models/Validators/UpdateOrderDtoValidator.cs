using FluentValidation;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Data;
using Microsoft.EntityFrameworkCore;
using ConfectioneryApi.Models;

namespace ConfectioneryApi.Validators
{
    public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
    {
        private readonly ConfectioneryDbContext _context;
        
        public UpdateOrderDtoValidator(ConfectioneryDbContext context)
        {
            _context = context;

            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("ID клієнта є обов'язковим.")
                .MustAsync(CustomerExists).WithMessage("Клієнта з таким ID не існує.");

    RuleFor(x => x.Status)
    .NotEmpty().WithMessage("Статус замовлення є обов'язковим.")
    // Перевіряємо, чи є рядок одним з імен в OrderStatus (без урахування регістру)
    .IsEnumName(typeof(OrderStatus), caseSensitive: false)
    .WithMessage("Передано недійсний тип статусу.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Замовлення повинно містити хоча б один виріб.");

            // Передаємо context у вкладений валідатор
            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemDtoValidator(_context));
        }

        private async Task<bool> CustomerExists(int customerId, CancellationToken token)
        {
            return await _context.Customers.AnyAsync(c => c.Id == customerId, token);
        }
    }
}