using FluentValidation;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Data; // Потрібно додати using для вашого DbContext
using Microsoft.EntityFrameworkCore;

namespace ConfectioneryApi.Validators
{
    public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
    {
        private readonly ConfectioneryDbContext _context;

        // Змінюємо конструктор для отримання DbContext
        public CreateCustomerDtoValidator(ConfectioneryDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ім'я клієнта є обов'язковим")
                .MaximumLength(100).WithMessage("Ім'я не може перевищувати 100 символів");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Телефон клієнта є обов'язковим")
                .Matches(@"^\+380\d{9}$").WithMessage("Некоректний формат номеру телефону. Очікується +380XXXXXXXXX")
                // Додаємо асинхронне правило для перевірки унікальності
                .MustAsync(BeUniquePhone).WithMessage("Клієнт з таким номером телефону вже існує.");
        }

        // Метод, що виконує перевірку унікальності в базі даних
        private async Task<bool> BeUniquePhone(string phone, CancellationToken cancellationToken)
        {
            // Правило поверне 'true' (валідація пройдена), якщо клієнта з таким телефоном НЕ знайдено
            return !await _context.Customers.AnyAsync(c => c.Phone == phone, cancellationToken);
        }
    }
}