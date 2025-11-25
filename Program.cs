using ConfectioneryApi.Data;
using ConfectioneryApi.Repositories;
using FluentValidation; 
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using ConfectioneryApi.Filters; 
using ConfectioneryApi.Configuration;
using ConfectioneryApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Додавання сервісів до контейнера.

// Налаштування бази даних для роботи з PostgreSQL
builder.Services.AddDbContext<ConfectioneryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Реєстрація репозиторіїв для ін'єкції залежностей
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Реєстрація сервісу бізнес-логіки
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPastryService, PastryService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// 2. Змінюємо реєстрацію контролерів, щоб додати наш фільтр
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AsyncValidationFilter>();
});

// Налаштування FluentValidation
// builder.Services.AddFluentValidationAutoValidation(); 
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

// Реєструємо "Шаблон Options" для нашого класу OrderSettings.
// Цей рядок "прив'язує" клас OrderSettings до секції "OrderSettings" в appsettings.json
// і реєструє його в системі ін'єкції залежностей (IoC).
builder.Services.Configure<OrderSettings>(
    builder.Configuration.GetSection("OrderSettings")
);

// Реєструємо сервіс кешування у пам'яті (IMemoryCache) в IoC-контейнері.
// Тепер ми можемо запитувати IMemoryCache через ін'єкцію залежностей.
builder.Services.AddMemoryCache();

var app = builder.Build();

// 2. Налаштування конвеєра обробки HTTP-запитів.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Автоматичне застосування міграцій при запуску
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ConfectioneryDbContext>();
    context.Database.Migrate(); 
}

app.Run();