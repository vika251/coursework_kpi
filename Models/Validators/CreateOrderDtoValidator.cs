using FluentValidation;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfectioneryApi.Validators
{
    public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
    {
        private readonly ConfectioneryDbContext _context;

        public CreateOrderDtoValidator(ConfectioneryDbContext context)
        {
            _context = context;

            RuleFor(x => x.CustomerId)
                .NotEmpty().WithMessage("ID клієнта є обов'язковим.")
                // Додаємо перевірку на існування клієнта.
                .MustAsync(CustomerExists).WithMessage("Клієнта з таким ID не існує.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Статус замовлення є обов'язковим.")
                // Дозволяємо створювати замовлення лише зі статусом 'New'.
                .Must(status => status == "Нове").WithMessage("При створенні замовлення статус може бути лише 'Нове'.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Замовлення повинно містити хоча б один виріб.");
            
            // Застосувати валідатор для кожної позиції в замовленні.
            // Важливо передати DbContext, щоб вкладений валідатор міг працювати з БД.
            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemDtoValidator(_context));
        }

        // Асинхронний метод для перевірки існування клієнта.
        private async Task<bool> CustomerExists(int customerId, CancellationToken token)
        {
            return await _context.Customers.AnyAsync(c => c.Id == customerId, token);
        }
    }
}