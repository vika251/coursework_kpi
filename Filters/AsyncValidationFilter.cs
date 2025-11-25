using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FluentValidation;
using System.Linq;
using System.Threading.Tasks;

namespace ConfectioneryApi.Filters
{
    public class AsyncValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var dto = context.ActionArguments.Values.FirstOrDefault();

            // Якщо аргумент відсутній, або це не клас (тобто це int, bool і т.д.), або це рядок,
            // то ми ігноруємо валідацію і просто переходимо до наступного кроку.
            if (dto == null || !dto.GetType().IsClass || dto is string)
            {
                await next();
                return;
            }
            // Отримуємо відповідний валідатор з контейнера служб.

            var validatorType = typeof(IValidator<>).MakeGenericType(dto.GetType());
            var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(dto);
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    context.Result = new BadRequestObjectResult(validationResult.ToDictionary());
                    return;
                }
            }
            
            await next();
        }
    }
}