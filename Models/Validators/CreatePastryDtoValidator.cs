using FluentValidation;
using ConfectioneryApi.Dtos;
using ConfectioneryApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfectioneryApi.Validators
{
    public class CreatePastryDtoValidator : AbstractValidator<CreatePastryDto>
    {
        private readonly ConfectioneryDbContext _context;

        public CreatePastryDtoValidator(ConfectioneryDbContext context)
        {
            _context = context;

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Назва виробу є обов'язковою.")
                .MaximumLength(150).WithMessage("Назва виробу не може перевищувати 150 символів.")
                // Додаємо правило на унікальність
                .MustAsync(BeUniquePastryName).WithMessage("Виріб з такою назвою вже існує.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Ціна повинна бути більшою за 0.")
                .LessThan(10000).WithMessage("Ціна повинна бути меншою за 10000.");
        }

        private async Task<bool> BeUniquePastryName(string name, CancellationToken cancellationToken)
        {
            // Перевіряємо, чи існує в базі виріб з такою назвою
            return !await _context.Pastries.AnyAsync(p => p.Name == name, cancellationToken);
        }
    }
}