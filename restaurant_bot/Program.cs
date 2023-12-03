using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using restaurant_bot.Brokers.Storages;
using restaurant_bot.Brokers.Telegrams;
using restaurant_bot.Services.Foundations.Dishes;
using restaurant_bot.Services.Foundations.Orders;
using restaurant_bot.Services.Foundations.Reviews;
using restaurant_bot.Services.Foundations.Telegrams;
using restaurant_bot.Services.Foundations.Users;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddControllers();
builder.Services.AddDbContext<IStorageBroker, StorageBroker>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IOrderService, OrderService>();
builder.Services.AddTransient<IDishService, DishService>();
builder.Services.AddTransient<IReviewService, ReviewService>();
builder.Services.AddSingleton<ITelegramBotClient>
    (new TelegramBotClient("6791582951:AAHxUSKIwmC1p49kR-KTM5tCSNMUOQ9EWmY"));
builder.Services.AddScoped<ITelegramBroker, TelegramBroker>();
builder.Services.AddScoped<ITelegramService, TelegramService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

using (var scope = scopeFactory.CreateScope())
{
    var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();

    telegramService.StartListening();

}

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
