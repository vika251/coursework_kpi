using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection; 
using System; 
using FluentValidation.AspNetCore;

namespace ConfectioneryApi.Filters
{
    public class AsyncValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // ВИПРАВЛЕННЯ: Шукаємо перший аргумент, який є класом (DTO) і не є рядком,
            // ігноруючи аргументи маршруту, як-от 'int id'.
            var dtoToValidate = context.ActionArguments.Values
                .FirstOrDefault(arg => 
                    arg != null && 
                    arg.GetType().IsClass && 
                    arg is not string);
            
            // Якщо DTO не знайдено, пропускаємо фільтр.
            if (dtoToValidate == null)
            {
                await next();
                return;
            }

            // Отримуємо відповідний валідатор з контейнера служб.
            var validatorType = typeof(IValidator<>).MakeGenericType(dtoToValidate.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                // Створюємо ValidationContext з коректним типом DTO, використовуючи рефлексію.
                var validationContextType = typeof(ValidationContext<>).MakeGenericType(dtoToValidate.GetType());
                var validationContext = (IValidationContext)Activator.CreateInstance(validationContextType, dtoToValidate)!;
                
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    // Повертаємо 400 Bad Request з повідомленням(ями) про помилку валідації.
                    context.Result = new BadRequestObjectResult(validationResult.ToDictionary());
                    return;
                }
            }
            
            await next();
        }
    }
}