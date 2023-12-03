//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Brokers.Telegrams;
using restaurant_bot.Services.Foundations.Dishes;
using restaurant_bot.Services.Foundations.Orders;
using restaurant_bot.Services.Foundations.Reviews;
using restaurant_bot.Services.Foundations.Users;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace restaurant_bot.Services.Foundations.Telegrams
{
    public partial class TelegramService : ITelegramService
    {
        private int selectedQuantity;
        private int paymentMethodPopCount = 0;

        private Message Message { get; set; }
        private long ChatId { get; set; }
        private string Text { get; set; }
        public Voice Voice { get; set; }

        private readonly ITelegramBotClient botClient;
        private readonly ITelegramBroker telegramBroker;
        private readonly IUserService userService;
        private readonly IOrderService orderService;
        private readonly IDishService dishService;
        private readonly IReviewService reviewService;

        public TelegramService(
            ITelegramBroker telegramBroker,
            IUserService userService,
            IOrderService orderService,
            IDishService dishService,
            IReviewService reviewService)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                    .CreateLogger();

            string token = "6791582951:AAHxUSKIwmC1p49kR-KTM5tCSNMUOQ9EWmY";
            this.botClient = new TelegramBotClient(token);
            this.telegramBroker = telegramBroker;
            this.userService = userService;
            this.orderService = orderService;
            this.dishService = dishService;
            this.reviewService = reviewService;
        }

        public void StartListening()
        {
            botClient.StartReceiving(MessageHandler, ErrorHandler);
        }

        private async Task MessageHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Message is not null)
            {
                await HandleStartSectionCommands();

                await HandleLanguageSectionCommands();

                // Russian language
                await HandleRussianLanguage(update);
            }
        }

        // Handle start section
        private async Task HandleStartSectionCommands()
        {
            var user = RetrieveUser;

            if (user is null)
            {
                switch (Text)
                {

                    case "/start":
                        await HandleStartCommandRu();
                        break;
                }
            }

        }

        private async Task HandleLanguageSectionCommands()
        {
            var user = RetrieveUser();

            if (user == null)
            {
                switch (Text)
                {

                    case "🇷🇺 Русский":
                        await HandleRussianLanguageRu();
                        break;
                    case "🇺🇿 O'zbekcha":
                        await HandleRussianLanguageRu();
                        break;
                    case "🇬🇧 English":
                        await HandleRussianLanguageRu();
                        break;
                }
            }
        }


        // Handle errors
        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Log.Error(exception, "Error in Telegram bot");

            if (exception is ApiRequestException apiRequestException)
            {
                Log.Error($"Telegram API Exception - ErrorCode: {apiRequestException.ErrorCode}");
            }
            else
            {
                Log.Error($"Unknown Exception: {exception.GetType().Name}");
            }

            long userId = ChatId;
            await client.SendTextMessageAsync(userId, "An error occurred. Please try again later.", cancellationToken: token);
        }
    }
}
