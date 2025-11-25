using FluentValidation;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfectioneryApi.Validators
{
    public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
    {
        private readonly ConfectioneryDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdateCustomerDtoValidator(ConfectioneryDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ім'я клієнта є обов'язковим")
                .MaximumLength(100).WithMessage("Ім'я не може перевищувати 100 символів");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Телефон клієнта є обов'язковим")
                .Matches(@"^\+380\d{9}$").WithMessage("Некоректний формат номеру телефону. Очікується +380XXXXXXXXX")
                // Створюємо комплексне асинхронне правило
                .MustAsync(BeUniquePhoneForUpdate).WithMessage("Клієнт з таким номером телефону вже існує.");
        }

        private async Task<bool> BeUniquePhoneForUpdate(string phone, CancellationToken cancellationToken)
        {
            // 1. Отримуємо Id з поточного HTTP-запиту
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return false; // Якщо контекст недоступний, валідація не пройде

            // Витягуємо 'id' з URL (наприклад, /api/customers/123)
            if (!int.TryParse(httpContext.Request.RouteValues["id"]?.ToString(), out var customerId))
            {
                // Якщо не змогли отримати Id з маршруту, вважаємо валідацію неуспішною
                return false;
            }

            // 2. Виконуємо перевірку в базі даних, виключаючи поточного клієнта
            return !await _context.Customers
                .AnyAsync(c => c.Phone == phone && c.Id != customerId, cancellationToken);
        }
    }
}