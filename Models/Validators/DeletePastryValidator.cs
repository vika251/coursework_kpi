using FluentValidation;
using ConfectioneryApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfectioneryApi.Validators
{
    public class DeletePastryValidator : AbstractValidator<int>
    {
        private readonly ConfectioneryDbContext _context;

        public DeletePastryValidator(ConfectioneryDbContext context)
        {
            _context = context;

            RuleFor(pastryId => pastryId)
                .MustAsync(NotBeUsedInOrders)
                .WithMessage("Неможливо видалити виріб, оскільки він є частиною існуючих замовлень.");
        }

        private async Task<bool> NotBeUsedInOrders(int pastryId, CancellationToken cancellationToken)
        {
            // Поверне 'true' (валідація успішна), якщо виріб НЕ використовується в замовленнях.
            bool isUsed = await _context.OrderItems.AnyAsync(oi => oi.PastryId == pastryId, cancellationToken);
            return !isUsed;
        }
    }
}