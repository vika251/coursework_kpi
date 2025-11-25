using FluentValidation;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Data; 
using Microsoft.EntityFrameworkCore;

namespace ConfectioneryApi.Validators
{
    public class UpdatePastryDtoValidator : AbstractValidator<UpdatePastryDto>
    {
        private readonly ConfectioneryDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UpdatePastryDtoValidator(ConfectioneryDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва виробу є обов'язковою.")
                .MaximumLength(150).WithMessage("Назва виробу не може перевищувати 150 символів.")
                // Додаємо комплексне правило на унікальність при оновленні
                .MustAsync(BeUniquePastryNameForUpdate).WithMessage("Виріб з такою назвою вже існує.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Ціна повинна бути більшою за 0.")
                .LessThan(10000).WithMessage("Ціна повинна бути меншою за 10000.");
        }

        private async Task<bool> BeUniquePastryNameForUpdate(string name, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return false;

            if (!int.TryParse(httpContext.Request.RouteValues["id"]?.ToString(), out var pastryId))
            {
                return false;
            }

            // Перевіряємо, чи існує інший виріб (з іншим Id) з такою ж назвою
            return !await _context.Pastries
                .AnyAsync(p => p.Name == name && p.Id != pastryId, cancellationToken);
        }
    }
}