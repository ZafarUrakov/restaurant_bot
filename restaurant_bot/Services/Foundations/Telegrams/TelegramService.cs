//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Brokers.Telegrams;
using restaurant_bot.Models.Dishes;
using restaurant_bot.Models.Orders;
using restaurant_bot.Models.Reviews;
using restaurant_bot.Services.Foundations.Dishes;
using restaurant_bot.Services.Foundations.Orders;
using restaurant_bot.Services.Foundations.Reviews;
using restaurant_bot.Services.Foundations.Users;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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


        private readonly Stack<(string message,
            ReplyKeyboardMarkup markup)> menuStack = new Stack<(string, ReplyKeyboardMarkup)>();

        private Dictionary<string, decimal> basket = new Dictionary<string, decimal>();

        private static readonly Dictionary<string?, decimal> prices = new Dictionary<string?, decimal>
        {
            { "Бизнес-ланч № 1", 60000 },
            { "Бизнес-ланч № 2", 65000 },
            { "Бизнес-ланч № 3", 68000 },
            { "Самса с говядиной", 60000 },
            { "Самса с курицей", 60000 },
            { "Самса с фаршом", 60000 },
            { "Говядина кусковой", 100000 },
            { "Баранина кусковой", 100000 },
            { "Люля Кебаб", 100000 },
            { "Ташкентский плов", 150000 },
            { "Ферганский плов", 150000 },
            { "Самаркандский плов", 150000 },
            { "Овощной", 80000 },
            { "Цезарь", 80000 },
            { "Оливье", 80000 },
            { "Чай черный", 20000 },
            { "Чай зеленый", 20000 },
            { "Чай молочный", 20000 },
            { "Американо", 80000 },
            { "Капучино", 80000 },
            { "Латте", 80000 },
            { "Вода газированная", 20000 },
            { "Вода негазированная", 20000 },
        };

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
            try
            {
                if (update.Message is not null)
                {
                    if (update.Message.Voice is not null)
                    {
                        Voice = update.Message.Voice;
                    }

                    Message = update.Message;
                    ChatId = update.Message.Chat.Id;
                    Text = update.Message.Text;

                    if (Text is not null)
                    {
                        await HandleTextMessageRu();
                    }
                    else if (update.Message.Contact?.PhoneNumber is not null)
                    {
                        await HandleContactMessageRu(update.Message.Contact);
                    }
                    else if (update.Message.Location?.Latitude is not null)
                    {
                        await HandleLocationMessageRu(update.Message.Location);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in MessageHandler: {ex}");
            }
        }


        // Handle TEXT Message
        private async Task HandleTextMessageRu()
         {
            await HandleStartSectionCommands();

            await HandleLanguageSectionCommands();

            await HandlePhoneNumberSectionCommandsRu();

            await HandleOrderSectionCommandsRu();

            await UpdateUserStatusBasedOnPriceRu();

            await HandleBranchesCommandsRu();

            await HandleMenuCommandRu();

            await ProcessBasketActionRu();

            await HandleDishSelectionRu();

            await HandleCommentMessageRu();

            await HandleCreateOrderSectionRu();

            await HandleBackToMenuSectionRu();

            await HandlePaymentSectionRu();

            await HandleReviewMessageRu();

            await HandleSettingsSectionRu();

            await HandleInputForPhoneNumberRu(Text);

            await HandleInputForNameRu(Text);

            //else if (menuStack.Peek().message == "🇷🇺 Выберите язык"
            //    || menuStack.Peek().message == "🇺🇿 Tilni tanlang"
            //    || menuStack.Peek().message == "🇬🇧 Select language")
            //{
            //    await HandleBackCommand();
            //}
        }


        // Function to check if the input is one of the predefined button titles
        private bool IsButtonTitleRu(string input)
        {
            return input == "😊Все понравилось, на 5 ❤️"
                || input == "☺️Нормально, на 4 ⭐️⭐️⭐️⭐️"
                || input == "😐Удовлетворительно на 3 ⭐️⭐️⭐️"
                || input == "☹️Не понравилось, на 2 ⭐️⭐️"
                || input == "😤Хочу пожаловаться 👎🏻"
                || input == "Изменить ФИО"
                || input == "Изменить номер"
                || input == "🇬🇧 Select language"
                || input == "🇺🇿 Tilni tanlang"
                || input == "🇷🇺 Выберите язык";
        }

        // Handle setting section
        private async Task HandleSettingsSectionRu()
        {
            switch (Text)
            {
                case "Изменить ФИО":
                    await HandleChangeNameRu();
                    break;
                case "Изменить номер":
                    break;
                case "🇷🇺 Выберите язык":
                    break;
            }
        }

        // Handle all dishes
        private async Task HandleDishSelectionRu()
        {
            switch (Text)
            {

                case "Бизнес-ланч № 1":
                    await SendBusinessLunchNumberOneInformationRu();
                    break;
                case "Бизнес-ланч № 2":
                    await SendBusinessLunchNumberTwoInformationRu();
                    break;
                case "Бизнес-ланч № 3":
                    await SendBusinessLunchNumberThreeInformationRu();
                    break;
                case "Самса с говядиной":
                    await SendSamsaWithBeefInformationRu();
                    break;
                case "Самса с курицей":
                    await SendSamsaWithChikenInformationRu();
                    break;
                case "Самса с фаршом":
                    await SendSamsaWithGroundMeatInformationRu();
                    break;
                case "Говядина кусковой":
                    await SendKebabWithBeefMeatInformationRu();
                    break;
                case "Баранина кусковой":
                    await SendKebabWithMuttonMeatInformationRu();
                    break;
                case "Люля Кебаб":
                    await SendLulaKebabInformationRu();
                    break;
                case "Ташкентский плов":
                    await SendPlovTashkentInformationRu();
                    break;
                case "Ферганский плов":
                    await SendPlovFerganaInformationRu();
                    break;
                case "Самаркандский плов":
                    await SendPlovSamarkandInformationRu();
                    break;
                case "Овощной":
                    await SendSalatOvoshnoyInformationRu();
                    break;
                case "Цезарь":
                    await SendSalatCezarInformationRu();
                    break;
                case "Оливье":
                    await SendSalatOlivieInformationRu();
                    break;
                case "Чай черный":
                    await SendBlackTeaInformationRu();
                    break;
                case "Чай зеленый":
                    await SendGreenTeaInformationRu();
                    break;
                case "Чай молочный":
                    await SendMilkTeaInformationRu();
                    break;
                case "Латте":
                    await SendLatteInformationRu();
                    break;
                case "Капучино":
                    await SendKapuchinoInformationRu();
                    break;
                case "Американо":
                    await SendAmericanoInformationRu();
                    break;
                case "Вода негазированная":
                    await SendWaterInformationRu();
                    break;
                case "Вода газированная":
                    await SendWaterGAZInformationRu();
                    break;
            }
        }

        // Backet processes
        private async Task ProcessBasketActionRu()
        {
            switch (Text)
            {
                case "📥 Корзина":
                    await SendBasketInformationRu();
                    break;
                case "🔄 Очистить":
                    await RemoveAllDishesFromBasketRu();
                    break;
                case
                string case1 when case1.StartsWith("❌ "):
                    string separatedPart = case1.Substring(1).TrimStart();
                    if (separatedPart == "Отменить")
                    {
                        return;
                    }
                    await RemoveDishesFromBasketStartingWithRu(separatedPart);
                    break;
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    await HandleQuantityButtonPressRu(Text);
                    break;
            }
        }

        // Remove dishes
        private async Task RemoveAllDishesFromBasketRu()
        {
            List<string> keysToRemove = new List<string>();

            foreach (var item in basket)
            {
                if (item.Key is not null)
                {
                    keysToRemove.Add(item.Key);
                }
            }

            foreach (var keyToRemove in keysToRemove)
            {
                basket.Remove(keyToRemove);
            }

            await SendBasketInformationIfDishesDeletedRu();

        }

        private async Task RemoveDishesFromBasketStartingWithRu(string dishForDelete)
        {
            basket.Remove(dishForDelete);

            await SendBasketInformationIfDishesDeletedRu();
        }


        private async Task HandleStartSectionCommands()
        {
            var user = RetrieveUser();

            if (user is null)
            {
                switch (Text)
                {

                    case "/start":
                        await HandleStartCommandRu();
                        break;
                }

                if (Text != "🇷🇺 Русский"
                     && Text != "/start"
                     && Text != "🇬🇧 English"
                     && Text != "🇺🇿 O'zbekcha")
                {
                    await SendMessageAsync("Произошло обновление базы данных. Введите или нажмите /start");

                    Text = "";
                }
            }

          

            else if (Text is "/start")
            {
                await ComeToMainAgainRu();
            }
            else
            {
                await HandleMainSectionCommandsRu();
            }
        }


        // Handle phone number section
        private async Task HandlePhoneNumberSectionCommandsRu()
        {
            var user = RetrieveUser();

            if (user is null)
            {
                if (IsCommandExpectedInCurrentPhoneNumberSectionRu(Text))
                {
                    if (IsPhoneNumberValidRu(Text))
                    {
                        await HandleContactWithouShareMessageRu(Text);
                    }
                }
            }
        }
        private bool IsPhoneNumberValidRu(string text)
        {
            return text.StartsWith("+") && text.Count(char.IsDigit) > 11;
        }
        private bool IsCommandExpectedInCurrentPhoneNumberSectionRu(string command)
        {
            return IsPhoneNumberValidRu(command);
        }


        // Handle main section
        private async Task HandleMainSectionCommandsRu()
        {
            if (IsCommandExpectedInCurrentMainSectionRu(Text))
            {
                switch (Text)
                {

                    case "🛍 Заказать":
                        await HandleOrderCommandRu();
                        break;
                    case "✍️ Оставить отзыв":
                        await HandleReviewCommandRu();
                        break;
                    case "☎️ Связаться с нами":
                        await SendContactSupportMessageRu();
                        break;
                    case "ℹ️ Информация":
                        await SendInformationMessageRu();
                        break;
                    case "⚙️ Настройки":
                        await HandleSettingsCommandRu();
                        break;
                }
            }
            else
            {
                return;
            }
        }
        private bool IsCommandExpectedInCurrentMainSectionRu(string command)
        {
            List<string> expectedCommands = GetExpectedCommandsForCurrentMainSectionRu();
            return expectedCommands.Contains(command);
        }
        private List<string> GetExpectedCommandsForCurrentMainSectionRu()
        {
            return new List<string> { "🛍 Заказать", "✍️ Оставить отзыв",
                "☎️ Связаться с нами", "ℹ️ Информация", "⚙️ Настройки" };
        }


        // Handle comment
        private async Task HandleCommentMessageRu()
        {
            var user = RetrieveUser();

            if (user != null)
            {
                var order = RetrieveOrderByUserIdAsync(user.Id);

                if (order is not null)
                {

                    var menuStackMessage = menuStack.Peek().message;

                    if (menuStackMessage == "Напишите комментарии к заказу")
                    {
                        if (Text != "⬅️ Назад" && Text != "⬅️ Меню" && Text != "❌ Отменить" && Text != "💵 Наличные")
                        {
                            if (Text == "Комментариев нет")
                                order.Comment = String.Empty;
                            else
                                order.Comment = Text;

                            var markup = await CreatePaymentMarkupAsyncLayerOneRu();

                            string message = "Выберите способ оплаты за Ваш заказ";

                            await SendMessagesWithMarkupAsync(message, markup);

                            menuStack.Push((message, markup));
                        }
                    }
                    else if (menuStackMessage == "Напишите кoмментарии к заказу")
                    {
                        if (Text != "⬅️ Назад" && Text != "⬅️ Мeню" && Text != "❌ Отменить" && Text != "💵 Наличные")
                        {
                            if (Text == "Кoмментариев нет")
                                order.Comment = String.Empty;
                            else
                                order.Comment = Text;

                            var markup = await CreatePaymentMarkupAsyncLayerTwoRu();

                            string message = "Выберите способ оплаты за Ваш заказ";

                            await SendMessagesWithMarkupAsync(message, markup);

                            menuStack.Push((message, markup));
                        }
                    }

                    await ModifyOrderAsync(order);
                }
                else
                {
                    var newOrder = new Order
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id
                    };

                    await this.orderService.AddOrderAsync(newOrder);
                }
            }
        }

        // Handle create order section
        private async Task HandleCreateOrderSectionRu()
        {
            switch (Text)
            {
                case "🚖 Оформить заказ":
                    await CreatePlaceOrderMarkupLayerOneRu();
                    break;
                case "🚖 Oформить заказ":
                    await CreatePlaceOrderMarkupLayerTwoRu();
                    break;
            }
        }

        // Handle back to menu section
        private async Task HandleBackToMenuSectionRu()
        {
            switch (Text)
            {

                case "⬅️ Меню":
                    await HandleBackToMenuCommandLayerOneRu();
                    break;
                case "⬅️ Мeню":
                    await HandleBackToMenuCommandLayerTwoRu();
                    break;
            }
        }

        // Handle payment section
        private async Task HandlePaymentSectionRu()
        {
            switch (Text)
            {

                case "💵 Наличные":
                case "💳 Click":
                case "💳 Payme":
                    await SendPayedMarkupRu();
                    break;
                case "❌ Отменить":
                    await HandleBackCommandRu();
                    break;
                case "✅ Заказываю":
                case "✅ Оплачено":
                    await SendOrderConfirmationMessageAsyncRu();
                    break;
            }
        }

        // Handle review message
        private async Task HandleReviewMessageRu()
        {
            switch (Text)
            {

                case "😤Хочу пожаловаться 👎🏻":
                case "☹️Не понравилось, на 2 ⭐️⭐️":
                case "😐Удовлетворительно на 3 ⭐️⭐️⭐️":
                case "☺️Нормально, на 4 ⭐️⭐️⭐️⭐️":
                case "😊Все понравилось, на 5 ❤️":
                    await AddReviewsAndSendMessageAsyncRu(Text);
                    break;
            }

            var user = RetrieveUser();

            if (user is not null)
            {
                if (menuStack.Peek().message == "Оставьте отзыв в виде сообщения или аудиосообщения")
                {
                    string text = Text.Trim();

                    if (!IsButtonTitleRu(text))
                    {
                        var review = this.reviewService.RetrieveAllReviews().FirstOrDefault(r => r.UserId == user.Id);

                        review.Message = text;

                        await this.reviewService.ModifyReviewAsync(review);

                        string message = "Спасибо за ваш отзыв!";

                        await SendMessageAsync(message);

                        await ComeToMainAgainRu();
                    }
                }
            }
        }

        // Handle input for name
        private async Task HandleInputForNameRu(string text)
        {
            var user = RetrieveUser();

            if (user is not null)
            {
                if (menuStack.Peek().message == "Введите имя")
                {
                    if (!IsButtonTitleRu(text))
                    {
                        user.FirstName = text;
                        await this.userService.ModifyUserAsync(user);
                        await HandleBackCommandRu();
                    }
                }
            }
        }

        // Handle for phone
        private async Task HandleInputForPhoneNumberRu(string text)
        {
            var user = RetrieveUser();

            if (user is not null)
            {
                if (menuStack.Peek().message == "Отправьте ваш номер телефона.")
                {
                    if (!IsButtonTitleRu(text))
                    {
                        user.PhoneNumber = text;
                        await this.userService.ModifyUserAsync(user);
                        await HandleBackCommandRu();
                    }
                }
            }
        }


        private async Task HandleChangeNameRu()
        {
            string message = "Введите имя";

            var markup = CreateChangeNameAndNumberMarkupRu();

            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandleChangeNumberRu()
        {
            string message = "Отправьте ваш номер телефона.";

            var markup = CreateChangeNameAndNumberMarkupRu();

            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandleChangeLanguageRu()
        {
            string message = "🇷🇺 Выберите язык";

            var markup = CreateChangeLanguageMarkupRu();

            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }



        // Handle other sections...
        private async Task HandleOrderSectionCommandsRu()
        {
            if (Text is not null)
            {
                switch (Text)
                {
                    case "⬅️ Назад" when menuStack.Count >= 1:
                        await HandleBackCommandRu();
                        break;
                    case "🚖 Доставка":
                        await HandleDeliveryCommandRu();
                        break;
                    case "🏃 Самовывоз":
                        await HandlePickupCommandRu();
                        break;
                }
            }
            else
            {
                return;
            }
        }

        private async Task HandleMenuCommandRu()
        {
            switch (Text)
            {
                case "Бизнес-ланчи":
                    await CreateBusinessLunchMarkupRu();
                    break;
                case "Cамса":
                    await CreateSomsaMarkupRu();
                    break;
                case "Плов":
                    await CreateOshMarkupRu();
                    break;
                case "Шашлыки":
                    await CreateKebabsMarkupRu();
                    break;
                case "Супы":
                    await CreateSoupsMarkupRu();
                    break;
                case "Салаты":
                    await CreateSaladsMarkupRu();
                    break;
                case "Чай":
                    await CreateTeaMarkupRu();
                    break;
                case "Кофе":
                    await CreateCoffeeMarkupRu();
                    break;
                case "Вода":
                    await CreateWaterMarkupRu();
                    break;
            }
        }

        private async Task HandleBranchesCommandsRu()
        {
            switch (Text)
            {
                case "Новза":
                case "ЦУМ":
                case "Гидрометцентр":
                case "Сергели":
                case "Кукча":
                    await HandleLocationSelectionRu();
                    break;
            }
        }

        private async Task HandleStartCommandRu()
        {
            string greetings = "Здравствуйте! Давайте для начала выберем язык обслуживания!\r\n\r\n" +
                               "Keling, avvaliga xizmat ko’rsatish tilini tanlab olaylik.\r\n\r\n" +
                               "Hi! Let's first we choose language of serving!";

            ReplyKeyboardMarkup markup = CreateLanguageMarkupRu();

            await SendMessagesWithMarkupAsync(greetings, markup);
        }

        private async Task HandleBackCommandRu()
        {
            if (menuStack.Count > 0)
            {
                var poppedItem = menuStack.Pop();

                if (poppedItem.message == "Продолжим? 😁")
                {
                    if (menuStack.Count > 0)
                    {
                        menuStack.Pop();
                        menuStack.Pop();

                        await SendMessagesWithMarkupAsync(poppedItem.message, poppedItem.markup);
                    }
                }
                else
                {
                    if (menuStack.Count == 0)
                    {
                        await ComeToMainAgainRu();
                    }
                    else
                    {
                        var poppedItem2 = menuStack.Peek();

                        if (poppedItem2.message == "Продолжим? ")
                        {
                            menuStack.Pop();
                        }
                        await SendMessagesWithMarkupAsync(poppedItem2.message, poppedItem2.markup);
                    }
                }
            }
        }

        private async Task HandleBackCommandIfDishSelectedRu()
        {

            if (menuStack.Count > 0)
            {
                //menuStack.Pop();

                var poppedItem = menuStack.Peek();

                await SendMessagesWithMarkupAsync(poppedItem.message, poppedItem.markup);

                //menuStack.Pop();
            }
        }

        private async Task HandleBackToMenuCommandLayerOneRu()
        {
            if (menuStack.Peek().message == "Выберите способ оплаты за Ваш заказ")
            {
                menuStack.Peek();
                menuStack.Pop();
                menuStack.Pop();
            }
            else
            {
                menuStack.Peek();
                menuStack.Pop();
            }

            ReplyKeyboardMarkup markup = CreateMenuMarkupRu();

            markup.ResizeKeyboard = true;

            string message = "Продолжим? 😁";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            if (menuStack.Count >= 2)
                menuStack.Pop();
        }
        private async Task HandleBackToMenuCommandLayerTwoRu()
        {
            if (menuStack.Peek().message == "Выберите способ оплаты за Ваш заказ")
            {
                menuStack.Peek();
                menuStack.Pop();
                menuStack.Pop();
                menuStack.Pop();
            }
            else
            {
                menuStack.Peek();
                menuStack.Pop();
                menuStack.Pop();
            }

            ReplyKeyboardMarkup markup = CreateMenuMarkupRu();

            markup.ResizeKeyboard = true;

            string message = "Продолжим? 😁";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            if (menuStack.Count >= 2)
                menuStack.Pop();
        }

        private async Task HandleRussianLanguageRu()
        {
            string greetings = "Добро пожаловать в Tarteeb restaurant!";
            string promptForPhoneNumber = "📱 Какой у Вас номер? Отправьте ваш номер телефона.\r\n\r\n" +
                                         "Чтобы отправить номер нажмите на кнопку \"📞 Поделиться контактом 📞";

            ReplyKeyboardMarkup markup =
                new ReplyKeyboardMarkup(KeyboardButton.WithRequestContact("📞 Поделиться контактом 📞"));
            markup.ResizeKeyboard = true;

            await SendMessageAsync(greetings);
            await SendMessagesWithMarkupAsync(promptForPhoneNumber, markup);
        }

        private async Task HandleOrderCommandRu()
        {
            ReplyKeyboardMarkup markup = CreateOrderMarkupRu();
            string message = $"Заберите свой заказ самостоятельно или выберите доставку";
            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandleSettingsCommandRu()
        {
            ReplyKeyboardMarkup markup = CreateSettingsMarkupRu();

            string message = $"⚙️ Настройки";

            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandleReviewCommandRu()
        {
            ReplyKeyboardMarkup markup = CreateReviewMarkupRu();

            string message = $"✅Контроль сервиса доставки Tarteeb\r\n" +
                $"Мы благодарим за сделанный выбор и будем рады, " +
                $"если Вы поможете улучшить качество нашего сервиса!\r\n" +
                $"Оцените нашу работу по 5 бальной шкале.";

            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));

        }

        private async Task SendContactSupportMessageRu()
        {
            ReplyKeyboardMarkup markup = CreateOrderMarkupRu();
            string message = $"Вы можете позвонить нам, если у вас есть вопросы: +998 90-865-08-59";
            await SendMessageAsync(message);
        }

        private async Task SendInformationMessageRu()
        {
            ReplyKeyboardMarkup markup = CreateOrderMarkupRu();
            string message = $"Мы недавно открылись, скоро вы сможете увидеть информацию";
            await SendMessageAsync(message);
        }

        private async Task HandleDeliveryCommandRu()
        {
            var user = RetrieveUser();

            if (user is not null)
            {
                var order = RetrieveOrderByUserIdAsync(user.Id);

                if (order is not null)
                {
                    order.OrderType = Text;

                    await ModifyOrderAsync(order);
                }
                else
                {
                    var newOrder = new Order
                    {
                        Id = Guid.NewGuid(),
                        OrderType = Text,
                        UserId = user.Id
                    };

                    await this.orderService.AddOrderAsync(newOrder);
                }


                ReplyKeyboardMarkup markup = CreateDeliveryMarkupRu();
                string message = "Куда нужно доставить ваш заказ 🚙?";
                await SendMessagesWithMarkupAsync(message, markup);

                menuStack.Push((message, markup));
            }
        }

        private async Task HandlePickupCommandRu()
        {
            var user = RetrieveUser();

            var order = RetrieveOrderByUserIdAsync(user.Id);

            if (order is not null)
            {
                order.OrderType = Text;

                await ModifyOrderAsync(order);
            }
            else
            {
                var newOrder = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderType = Text,
                    UserId = user.Id
                };

                await this.orderService.AddOrderAsync(newOrder);
            }

            ReplyKeyboardMarkup markup = CreatePickupMarkupRu();
            string message = "Где вы находитесь 👀?\r\nЕсли вы отправите локацию 📍, мы определим ближайший к вам филиал";
            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandleContactMessageRu(Contact contact)
        {
            if (contact?.PhoneNumber is not null)
            {
                Models.Users.User user = new Models.Users.User
                {
                    Id = Guid.NewGuid(),
                    TelegramId = ChatId,
                    Status = "Поделиться контактом",
                    FirstName = Message.Chat.FirstName,
                    LastName = Message.Chat.LastName,
                    PhoneNumber = contact.PhoneNumber,
                };

                Models.Users.User expectedUser = await userService.AddUserAsync(user);

                ReplyKeyboardMarkup markup = CreateMainMarkupRu();
                string firstMessage = $"Отлично, спасибо за регистрацию {expectedUser.FirstName} {expectedUser.LastName}  🥳\n\n";
                string secondMessage = $"Оформим заказ вместе? 😃";

                await SendMessageAsync(firstMessage);
                await SendMessagesWithMarkupAsync(secondMessage, markup);

            }
        }

        private async Task HandleContactWithouShareMessageRu(string contact)
        {
            if (contact is not null)
            {
                Models.Users.User user = new Models.Users.User
                {
                    Id = Guid.NewGuid(),
                    TelegramId = ChatId,
                    Status = "Поделиться контактом",
                    FirstName = Message.Chat.FirstName,
                    LastName = Message.Chat.LastName,
                    PhoneNumber = contact
                };

                Models.Users.User expectedUser = await userService.AddUserAsync(user);

                ReplyKeyboardMarkup markup = CreateMainMarkupRu();
                string firstMessage = $"Отлично, спасибо за регистрацию {expectedUser.FirstName} {expectedUser.LastName}  🥳\n\n";
                string secondMessage = $"Оформим заказ вместе? 😃";

                await SendMessageAsync(firstMessage);
                await SendMessagesWithMarkupAsync(secondMessage, markup);

                menuStack.Push((secondMessage, markup));
            }
        }

        private async Task HandleLocationMessageRu(Location location)
        {
            if (location?.Latitude is not null && location?.Longitude is not null)
            {
                var user = RetrieveUser();

                if (user is not null)
                {
                    string apiKey = "e2e8a7f702ae48b0b602f87993c98955";

                    string apiUrl = $"https://api.opencagedata.com/geocode/v1/json?key={apiKey}&q={location.Latitude}+{location.Longitude}";

                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            string jsonString = await response.Content.ReadAsStringAsync();
                            dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);

                            if (result.results.Count > 0)
                            {
                                string city = result.results[0].components.city;
                                string street = result.results[0].components.road;
                                string houseNumber = result.results[0].components.house_number;

                                string userLocation = $"{city} {street} {houseNumber} 📍";

                                string secondMessage = $"Рядом с вами есть филиалы в городе {userLocation}";
                                ReplyKeyboardMarkup markup = CreatePickupMarkupRu();
                                await SendMessagesWithMarkupAsync(secondMessage, markup);

                                user.Location = userLocation;

                                await this.userService.ModifyUserAsync(user);
                            }
                        }
                    }
                }
            }
        }

        private async Task HandleLocationSelectionRu()
        {
            ReplyKeyboardMarkup markup = CreateMenuMarkupRu();
            markup.ResizeKeyboard = true;

            string firstMessage = "С чего начнем?";
            string secondMessage = "Продолжим? 😉";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, firstMessage, markup);

            menuStack.Push((secondMessage, markup));
        }


        // Create some murkups
        private static ReplyKeyboardMarkup CreateLanguageMarkupRu()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[]
            {
                new KeyboardButton("🇺🇿 Uzbek"),
                new KeyboardButton("🇷🇺 Русский"),
                new KeyboardButton("🇺🇸 English")
            })
            {
                ResizeKeyboard = true
            };
        }

        private static ReplyKeyboardMarkup CreateOrderMarkupRu()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("🚖 Доставка"),
                    new KeyboardButton("🏃 Самовывоз")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };
        }

        private static ReplyKeyboardMarkup CreateSettingsMarkupRu()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("Изменить ФИО"),
                    new KeyboardButton("Изменить номер")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("🇷🇺 Выберите язык")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };
        }

        private static ReplyKeyboardMarkup CreateReviewMarkupRu()
        {
            // Individual buttons
            var button1 = new KeyboardButton("😊Все понравилось, на 5 ❤️");
            var button2 = new KeyboardButton("☺️Нормально, на 4 ⭐️⭐️⭐️⭐️");
            var button3 = new KeyboardButton("😐Удовлетворительно на 3 ⭐️⭐️⭐️");
            var button4 = new KeyboardButton("☹️Не понравилось, на 2 ⭐️⭐️");
            var button5 = new KeyboardButton("😤Хочу пожаловаться 👎🏻");
            var backButton = new KeyboardButton("⬅️ Назад");

            // Creating the ReplyKeyboardMarkup
            var replyMarkup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[] { button1},
                new KeyboardButton[] {button2},
                new KeyboardButton[] {button3},
                new KeyboardButton[] {button4},
                new KeyboardButton[] {button5 },
                new KeyboardButton[] { backButton }
            })
            {
                ResizeKeyboard = true
            };

            // Return the created ReplyKeyboardMarkup
            return replyMarkup;

        }

        private static ReplyKeyboardMarkup CreateDeliveryMarkupRu()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    KeyboardButton.WithRequestLocation("Определить ближайший филиал")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };
        }

        private static ReplyKeyboardMarkup CreatePickupMarkupRu()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    KeyboardButton.WithRequestLocation("Определить ближайший филиал")
                },
                new[]
                {
                    new KeyboardButton("Новза"),
                    new KeyboardButton("ЦУМ")
                },
                new[]
                {
                    new KeyboardButton("Гидрометцентр"),
                    new KeyboardButton("Сергели")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Кукча")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };
        }

        private static ReplyKeyboardMarkup CreateMainMarkupRu()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("🛍 Заказать")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("✍️ Оставить отзыв"),
                    new KeyboardButton("ℹ️ Информация")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("☎️ Связаться с нами"),
                    new KeyboardButton("⚙️ Настройки")
                }
            })
            {
                ResizeKeyboard = true
            };
        }

        private static ReplyKeyboardMarkup CreateMenuMarkupRu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📥 Корзина"), new KeyboardButton("🚖 Оформить заказ") },
                new[] { new KeyboardButton("Бизнес-ланчи"), new KeyboardButton("Cамса") },
                new[] { new KeyboardButton("Шашлыки"), new KeyboardButton("Плов") },
                new[] { new KeyboardButton("Салаты"), new KeyboardButton("Чай") },
                new[] { new KeyboardButton("Кофе"), new KeyboardButton("Вода") },
                new[] { new KeyboardButton("⬅️ Назад") },
            });
        }

        private static ReplyKeyboardMarkup CreateChangeNameAndNumberMarkupRu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("⬅️ Назад") }
            })
            {
                ResizeKeyboard = true
            };
        }

        private static ReplyKeyboardMarkup CreateChangeLanguageMarkupRu()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("🇷🇺 Русский"), new KeyboardButton("🇺🇿 O'zbekcha") },
                new[] { new KeyboardButton("🇬🇧 English") }
            })
            {
                ResizeKeyboard = true
            };
        }


        private Task<ReplyKeyboardMarkup> CreateBacketMarkupRu(Dictionary<string, decimal> dishes)
        {
            var buttons = new List<KeyboardButton[]>();

            foreach (var dish in dishes.Keys)
            {
                buttons.Add(new KeyboardButton[] { new KeyboardButton($"❌ {dish}") });
            }

            buttons.Add(new KeyboardButton[] { new KeyboardButton("⬅️ Назад"), new KeyboardButton("🔄 Очистить") });

            buttons.Add(new KeyboardButton[] { new KeyboardButton("🚖 Oформить заказ") });

            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(buttons.ToArray())
            {
                ResizeKeyboard = true
            };

            return Task.FromResult(markup);
        }


        //Menu information
        private async Task<ReplyKeyboardMarkup> CreateBusinessLunchMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Бизнес-ланч № 1"),
                    new KeyboardButton("Бизнес-ланч № 2")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Бизнес-ланч № 3")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSomsaMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Самса с говядиной"),
                    new KeyboardButton("Самса с курицей")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Самса с фаршом")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateOshMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Ташкентский плов"),
                    new KeyboardButton("Ферганский плов")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Самаркандский плов")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateKebabsMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Говядина кусковой"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Баранина кусковой"),
                    new KeyboardButton("Люля Кебаб")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSoupsMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Шурпа"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Лагман"),
                    new KeyboardButton("Мастава")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSaladsMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")

                },
                new[]
                {
                    new KeyboardButton("Овощной"),
                    new KeyboardButton("Цезарь")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Оливье")
                }
            })
            {
                ResizeKeyboard = true
            };
            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateTeaMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Чай черный"),
                    new KeyboardButton("Чай зеленый")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Чай молочный")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateCoffeeMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Американо"),
                    new KeyboardButton("Капучино")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Латте")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateWaterMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("📥 Корзина")
                },
                new[]
                {
                    new KeyboardButton("Вода газированная"),
                    new KeyboardButton("Вода негазированная")
                },
            })
            {
                ResizeKeyboard = true
            };

            await SendMenuInstructionRu(markup);

            return markup;
        }

        // Order and payment layer
        private async Task<ReplyKeyboardMarkup> CreatePlaceOrderMarkupLayerOneRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("Комментариев нет"),
                },
                new[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("⬅️ Меню")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendComentInstructionLayerOneRu(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreatePlaceOrderMarkupLayerTwoRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("Кoмментариев нет"),
                },
                new[]
                {
                    new KeyboardButton("⬅️ Назад"),
                    new KeyboardButton("⬅️ Мeню")
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendComentInstructionLayerTwoRu(markup);

            return markup;
        }

        private async Task<ReplyKeyboardMarkup> CreatePaymentMarkupAsyncLayerOneRu()
        {
            return await Task.Run(() =>
            {
                ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                {
            new KeyboardButton[]
            {
                new KeyboardButton("💵 Наличные"),
            },
            new[]
            {
                new KeyboardButton("💳 Payme"),
                new KeyboardButton("💳 Click")
            },
            new[]
            {
                new KeyboardButton("⬅️ Назад"),
                new KeyboardButton("⬅️ Меню")
            }
                })
                {
                    ResizeKeyboard = true
                };

                return markup;
            });
        }
        private async Task<ReplyKeyboardMarkup> CreatePaymentMarkupAsyncLayerTwoRu()
        {
            return await Task.Run(() =>
            {
                ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
                {
            new KeyboardButton[]
            {
                new KeyboardButton("💵 Наличные"),
            },
            new[]
            {
                new KeyboardButton("💳 Payme"),
                new KeyboardButton("💳 Click")
            },
            new[]
            {
                new KeyboardButton("⬅️ Назад"),
                new KeyboardButton("⬅️ Мeню")
            }
                })
                {
                    ResizeKeyboard = true
                };

                return markup;
            });
        }


        // Send order confirmation message
        private async Task SendOrderConfirmationMessageAsyncRu()
        {
            var user = RetrieveUser();

            if (user is not null)
            {
                var order = RetrieveOrderByUserIdAsync(user.Id);

                if (order.PaymentMethod == "💵 Наличные" || Text == "✅ Оплачено")
                {

                    string message = "Спасибо, ваш заказ принят, " +
                        "как только оператор его подтвердит, вы получите уведомление.";

                    await SendMessageAsync(message);

                    await ComeToMainAgainRu();

                    string sendMessage = "Ваш заказ подтвержден. Спасибо за покупку!";

                    System.Timers.Timer timer = new System.Timers.Timer(30000);
                    timer.Elapsed += async (sender, e) =>
                    {
                        await SendMessageAsync(sendMessage);

                        timer.Stop();

                    };

                    timer.Start();

                    if (order.OrderType == "🚖 Доставка")
                    {

                        string sendSecondMessage = $"Tarteeb restaurant\n\n" +
                            $"Приехал курьер Zafar и ожидает вас по адресу " +
                            $"{user.Location} \n Для связи с курьером @zafar_urakov \n" +
                            $"\n\n Спасибо за покупку 😊 \n\n Приятного аппетита 😋";

                        System.Timers.Timer secondTimer = new System.Timers.Timer(40000);
                        secondTimer.Elapsed += async (sender, e) =>
                        {
                            await SendMessageAsync(sendSecondMessage);

                            secondTimer.Stop();

                        };

                        secondTimer.Start();
                    }
                }
                else
                {
                    string message = "Ваш заказ создан, пожалуйста, оплатите его.";

                    var replyMarkup = await SendReadyOrderMessageRu();

                    await botClient.SendTextMessageAsync(
                        chatId: ChatId,
                        text: message,
                        replyMarkup: replyMarkup);

                    string message2 = $"Оплата через Click\r\nСумма к оплате: {order.TotalAmount} сум.\r\n" +
                        "Что бы оплатить нажмите на кнопку \"✅ Оплатить\".";

                    await botClient.SendTextMessageAsync(
                    chatId: ChatId,
                    text: message2,
                    replyMarkup: new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithUrl(
                            text: "✅ Оплатить",
                            url: "https://ru.wikipedia.org/wiki/Hello,_world!")));
                }
            }
            else
            {
                return;
            }
        }

        // Send ready order information
        private Task<ReplyKeyboardMarkup> SendReadyOrderMessageRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
            new KeyboardButton[]
            {
                new KeyboardButton("✅ Оплачено"),
            }
            })
            {
                ResizeKeyboard = true
            };

            return Task.FromResult(markup);
        }

        private async Task<ReplyKeyboardMarkup> SendPayedMarkupRu()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("✅ Заказываю"),
                },
                new[]
                {
                    new KeyboardButton("❌ Отменить"),
                }
            })
            {
                ResizeKeyboard = true
            };

            await SendReadyOrderInstructionRu(markup);

            return markup;
        }
        private async Task SendReadyOrderInstructionRu(ReplyKeyboardMarkup markup)
        {
            var user = RetrieveUser();

            if (user is not null)
            {
                StringBuilder basketInfo = new StringBuilder();

                var order = RetrieveOrderByUserIdAsync(user.Id);

                order.PaymentMethod = Text;

                basketInfo.AppendLine($"Тип заказа: {order.OrderType}\n" +
                                      $"Телефон: +{user.PhoneNumber}\n" +
                                      $"Способ оплаты: {order.PaymentMethod}\n" +
                                      $"Коментарий: {order.Comment}\n\n");

                await ProcessBasketItemAsyncRu(basketInfo, order);

                basketInfo.AppendLine($"Сумма: {CalculateTotalPriceRu():N0} сум");

                order.TotalAmount = CalculateTotalPriceRu();

                await ModifyOrderAsync(order);

                await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, basketInfo.ToString(), markup);

                basket.Clear();
            }
            else
            {
                return;
            }
        }

        private async Task ProcessBasketItemAsyncRu(StringBuilder basketInfo, Order order)
        {
            foreach (var item in basket)
            {
                decimal itemTotal = item.Value * prices[item.Key];
                basketInfo.AppendLine($"{item.Key}\n{item.Value} x {prices[item.Key]:N0} сум = {itemTotal:N0} сум\n");

                Dish dish = new Dish
                {
                    Id = Guid.NewGuid(),
                    Name = item.Key,
                    Price = prices[item.Key],
                    OrderId = order.Id
                };

                await this.dishService.AddDishAsync(dish);

                order.Dishes.Add(dish);
            }
        }

        // Send coment instruction
        private async Task SendComentInstructionLayerOneRu(ReplyKeyboardMarkup markup)
        {
            if (basket.Count == 0)
            {
                await this.telegramBroker.SendMessageAsync(ChatId, "Ваша корзина пуста");

                return;
            }
            else
            {
                string message = "Напишите комментарии к заказу";

                await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

                menuStack.Push((message, markup));
            }
        }
        private async Task SendComentInstructionLayerTwoRu(ReplyKeyboardMarkup markup)
        {
            if (basket.Count == 0)
            {
                await this.telegramBroker.SendMessageAsync(ChatId, "Ваша корзина пуста");

                return;
            }
            else
            {
                string message = "Напишите кoмментарии к заказу";

                await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

                menuStack.Push((message, markup));
            }
        }

        // Send menu instruction
        private async Task SendMenuInstructionRu(ReplyKeyboardMarkup markup)
        {
            string message = "Нажмите «⏬ Список » для ознакомления с меню или выберите блюдо";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));
        }

        // Keyboards markup number of dishes
        private static ReplyKeyboardMarkup GenerateCountKeyboardMarkupRu()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[] { new KeyboardButton("1"), new KeyboardButton("2"), new KeyboardButton("3") },
                new KeyboardButton[] { new KeyboardButton("4"), new KeyboardButton("5"), new KeyboardButton("6") },
                new KeyboardButton[] { new KeyboardButton("7"), new KeyboardButton("8"), new KeyboardButton("9") },
                new KeyboardButton[] { new KeyboardButton("📥 Корзина"), new KeyboardButton("⬅️ Назад") }
            })
            {
                ResizeKeyboard = true
            };
        }

        // Calculate total price
        private decimal CalculateTotalPriceRu()
        {
            decimal total = 0;
            foreach (var item in basket)
            {
                if (prices.TryGetValue(item.Key, out decimal price))
                {
                    total += price * item.Value;
                }
            }
            return total;
        }

        private async Task SendBasketInformationRu()
        {
            if (basket.Count == 0)
            {
                var emptyBacketMessage = "Ваша корзина пуста";

                await this.telegramBroker.SendMessageAsync(ChatId, emptyBacketMessage);

                return;
            }

            StringBuilder basketInfo = new StringBuilder("📥 Корзина:\n\n");

            foreach (var item in basket)
            {
                decimal itemTotal = item.Value * prices[item.Key];
                basketInfo.AppendLine($"{item.Key}\n{item.Value} x {prices[item.Key]:N0} сум = {itemTotal:N0} сум\n");
            }

            basketInfo.AppendLine($"Сумма: {CalculateTotalPriceRu():N0} сум");

            var markup = await CreateBacketMarkupRu(basket);

            string message = "*«❌ Наименование »* - удалить одну позицию \r\n " +
                "*«🔄 Очистить »* - полная очистка корзины";

            await this.telegramBroker.SendMessageAsync(ChatId, message);

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, basketInfo.ToString(), markup);

            menuStack.Push((basketInfo.ToString(), markup));

        }

        // Send basket information
        private async Task SendBasketInformationIfDishesDeletedRu()
        {
            if (basket.Count == 0)
            {
                var updatedMessage = "Ваша корзина пуста";

                var previousMenu = menuStack.Peek();

                var markup = await CreateBacketMarkupRu(basket);

                await SendMessagesWithMarkupAsync(updatedMessage, markup);

                return;
            }
            else
            {
                var updatedMessage = "Удалено";

                var previousMenu = menuStack.Peek();

                var markup = await CreateBacketMarkupRu(basket);


                await SendMessagesWithMarkupAsync(updatedMessage, markup);

                return;
            }
        }

        //Update status
        private async Task UpdateUserStatusBasedOnPriceRu()
        {
            if (prices != null)
            {
                foreach (var i in prices)
                {
                    if (i.Key == Text)
                    {
                        if (userService != null)
                        {
                            var user = this.userService.RetrieveAllUsers()
                                .FirstOrDefault(u => u.TelegramId == ChatId);

                            if (user != null)
                            {
                                user.Status = i.Key;
                                var modifiedUser = await this.userService.ModifyUserAsync(user);
                            }
                        }
                    }
                }
            }
        }

        // Add to basket
        private async Task HandleQuantityButtonPressRu(string quantity)
        {
            var user = RetrieveUser();

            if (user is not null)
            {
                var selectedDish = user.Status;

                if (string.IsNullOrEmpty(selectedDish))
                {
                    await this.telegramBroker.SendMessageAsync(ChatId, "Ошибка: Выберите блюдо сначала.");
                    return;
                }

                if (int.TryParse(quantity, out int selectedQuantity))
                {
                    await AddToBasketRu(selectedDish, selectedQuantity);

                    selectedDish = null;

                    this.selectedQuantity = 0;
                }
                else
                {
                    await this.telegramBroker.SendMessageAsync(ChatId,
                        "Некорректное количество. Пожалуйста, выберите количество с клавиатуры.");
                }
            }
            else
            {
                return;
            }
        }

        private async Task AddToBasketRu(string itemName, int quantity)
        {
            if (basket.ContainsKey(itemName))
            {
                basket[itemName] += quantity;
            }
            else
            {
                basket[itemName] = quantity;
            }

            await HandleBackCommandIfDishSelectedRu();
        }


        // Come to menu
        private async Task ComeToMainAgainRu()
        {
            ReplyKeyboardMarkup markup = CreateMainMarkupRu();
            string message = $"Продолжим ? 😃";

            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Clear();

            menuStack.Push((message, markup));
        }


        //Send dishes information
        private async Task SendBusinessLunchNumberOneInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://media-cdn.tripadvisor.com/media/photo-s/0e/ae/35/29/business-lunch.jpg"),
                "Бизнес-ланч № 1" +
                "Первое блюдо: Мержимек Ezo Gelin Çorbasi 1/2 порции.\r\n" +
                "Второе блюдо: Куриные котлеты с сыром с турецкой лепешкой Екмек.\r\n" +
                "Напиток: Лимонад ягодный 0.4 л.\r\n\r\nЦена: 60 000 сум"
            );
        }
        private async Task SendBusinessLunchNumberTwoInformationRu()
        {
            await SendDishInformationRu(
                 InputFile.FromUri("https://marhaba.qa/qatarlinks/wp-content/uploads/2020/07/Business-Lunch-Al-Baraha_Easy-Resize.com_.jpg"),
                "Бизнес-ланч № 2" +
                "Первое блюдо: куриный суп 1/2 порции.\r\n" +
                "Второе блюдо: шашлык говяжий с турецкой лепешкой Екмек.\r\n" +
                "Напиток: чай с лимоном.\r\n\r\nЦена: 65 000 сум"
            );
        }
        private async Task SendBusinessLunchNumberThreeInformationRu()
        {
            await SendDishInformationRu(
                 InputFile.FromUri("https://mado.az/pics/259/259/product/689/1_1677142202.jpg"),
                "Бизнес-ланч № 3" +
                "Основное блюдо: рыба с овощами 1/2 порция с турецкой лепешкой Екмек.\r\n" +
                "Салат на выбор: греческий 1/2 порция или свежий 1/2 порция.\r\n" +
                "Напиток: чай с лимоном.\r\n\r\nЦена: 68 000 сум"
            );
        }

        private async Task SendSamsaWithBeefInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://static.insales-cdn.com/images/products/1/2641/245615185/%D1%81%D0%B0%D0%BC%D1%81%D0%B0_%D1%81_%D0%B3%D0%BE%D0%B2%D1%8F%D0%B4.jpg"),
                "Эта вкуснейшая среднеазиатская выпечка будет подспорьем, " +
                "когда нужен питательный перекус или быстрый оригинальный ужин.\r\n\r\n" +
                "Самса с говядиной — полноценное сытное блюдо, которое позволит " +
                "накормить большую семью и голодную компанию неожиданных гостей. \r\n\r\nЦена: 60 000 сум"
            );
        }
        private async Task SendSamsaWithChikenInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://pudov.ru/upload/resize_cache/webp/iblock/6d4/715_500_1/9wjlwoo27rqa29mr0ain4y5ah96t7s09.webp"),
                "Самса с курицей – одна из самых знаменитых индийских закусок. " +
                "Хрустящее тесто, традиционные ароматные специи и много-много курочки внутри! " +
                "Нежная и пряная самса в Индии считается «уличной едой» (своего рода фаст-фудом). " +
                "Мы же можем приготовить ее в качестве закуски, дополнения к " +
                "ужину или снэка для вечеринки. Пошаговый рецепт приготовления домашней самсы с курицей –" +
                " для новаторов и истинных ценителей индийской кухни!\r\n\r\nЦена: 60 000 сум"
            );
        }
        private async Task SendSamsaWithGroundMeatInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://www.photorecept.ru/wp-content/uploads/2021/08/samsa-s-farshem-i-lukom-1.jpg"),
                "Самса с фаршем – одна из самых знаменитых индийских закусок. " +
                "Хрустящее тесто, традиционные ароматные специи и много-много курочки внутри! " +
                "Нежная и пряная самса в Индии считается «уличной едой» (своего рода фаст-фудом). " +
                "Мы же можем приготовить ее в качестве закуски, дополнения к " +
                "ужину или снэка для вечеринки. Пошаговый рецепт приготовления домашней самсы с курицей –" +
                " для новаторов и истинных ценителей индийской кухни!\r\n\r\nЦена: 60 000 сум"
            );
        }


        private async Task SendKebabWithBeefMeatInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://vsemangali.ru/upload/iblock/da5/da5f945c2203d53b50f384351a8e4fde.jpg"),
                "Шашлык из говядины — это искусное гастрономическое произведение, созданное для того, " +
                "чтобы вызывать восторг и наслаждение каждым укусом. Кусочки нежной говядины, " +
                "отобранные с особым вниманием, бережно нанизаны на шампуры. Мясо окутано маринадом, " +
                "который позволяет ему впитать в себя богатство пряных специй и свежести трав.\r\n\r\nЦена: 100 000 сум"
            );
        }
        private async Task SendKebabWithMuttonMeatInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://karfood.ru/wp-content/uploads/2021/06/604e794c3045c604e794c3045d.png"),
                "Шашлык из баранины — это апогей мужественности и страсти, запечатленный в " +
                "гастрономическом шедевре. Кусочки сочной и нежной баранины " +
                "вкраплены ветвистым жиросодержащим мрамором, который придает мясу неповторимую мягкость и " +
                "интенсивный вкус.\r\n\r\nЦена: 100 000 сум"
            );
        }
        private async Task SendLulaKebabInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://cdn-m.sport24.ru/m/e466/1ab1/2e0c/4a78/9eaf/de11/558a/9a71/1600_10000_max.jpeg"),
                "Люля-кебаб — это воплощение изысканности и изящества в мире кулинарных наслаждений. " +
                "Кусочки нежной и сочной баранины, тщательно отобранные и приготовленные, становятся центром внимания на шампурах. " +
                "Мясо пронизано ароматами пряных специй и свежих трав, придавая ему уникальный и интригующий характер.\r\n\r\nЦена: 100 000 сум"
            );
        }


        private async Task SendPlovTashkentInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://myday.uz/images/social_photo/17381.jpg"),
                "Плов — блюдо восточной кухни, основу которого составляет варёный рис. " +
                "Отличительным свойством плова является его рассыпчатость, достигаемая соблюдением технологии приготовления " +
                "риса и добавлением в плов животного или растительного жира, препятствующего слипанию крупинок.\r\n\r\nЦена: 150 000 сум"
            );
        }
        private async Task SendPlovFerganaInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://www.maggi.ru/data/images/recept/img640x500/recept_1514_u61i.jpg"),
                "Плов — блюдо восточной кухни, основу которого составляет варёный рис. " +
                "Отличительным свойством плова является его рассыпчатость, достигаемая соблюдением технологии приготовления " +
                "риса и добавлением в плов животного или растительного жира, препятствующего слипанию крупинок.\r\n\r\nЦена: 150 000 сум"
            );
        }
        private async Task SendPlovSamarkandInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://s1.eda.ru/StaticContent/Photos/130410124428/150630142514/p_O.jpg"),
                "Плов — блюдо восточной кухни, основу которого составляет варёный рис. " +
                "Отличительным свойством плова является его рассыпчатость, достигаемая соблюдением технологии приготовления " +
                "риса и добавлением в плов животного или растительного жира, препятствующего слипанию крупинок.\r\n\r\nЦена: 150 000 сум"
            );
        }


        private async Task SendSalatOvoshnoyInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://www.maggi.ru/data/images/recept/img640x500/recept_1868_con5.jpg"),
                "Томат, огурец, микс зелени, ореховый соус, орех грецкий дробленый, лук красный.\r\n\r\nЦена: 80 000 сум"
            );
        }
        private async Task SendSalatCezarInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://imgproxy.sbermarket.ru/imgproxy/size-210-210/czM6Ly9jb250ZW50LWltYWdlcy1wcm9kL3Byb2R1Y3RzLzIzMDcyNDA3L29yaWdpbmFsLzEvMjAyMy0wMy0xN1QxMSUzQTA0JTNBMjQuMjA4MDAwJTJCMDAlM0EwMC8yMzA3MjQwN18xLmpwZw==.jpg"),
                "\r\nгренки пшеничные\r\nсодержат витамины группы B\r\nпомидоры черри\r\nбогаты антиоксидантами\r\n" +
                "салат айсберг\r\nисточник антиоксидантов\r\nсоус цезарь\r\nбогат полиненасыщенными жирными кислотами\r\n" +
                "Описание\r\nОдин из самых популярных салатов идеально впишется на любой праздничный стол. Салат " +
                "\"Цезарь с курицей\" с соусом по оригинальному рецепту точно понравится вашим гостям.\r\n\r\nЦена: 80 000 сум"
            );
        }
        private async Task SendSalatOlivieInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://user36270.clients-cdnnow.ru/1690554670803-350x244.jpeg"),
                "Куриное филе, картофель, маринованные огурцы, свежие огурцы, зеленый горошек, куриное яйцо, майонез, укроп\r\n\r\nЦена: 80 000 сум"
            );
        }


        private async Task SendAmericanoInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://solocoffee.su/image/catalog/recept-kofe-amerikano-v-turke.jpg"),
                "Бодрящий американо по узбекски \r\n\r\nЦена: 80 000 сум"
            );
        }
        private async Task SendKapuchinoInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://safiabakery.uz/uploads/products/171_1677931968.jpg"),
                "Бодрящий капучино по узбекски \r\n\r\nЦена: 80 000 сум"
            );
        }
        private async Task SendLatteInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://upload.wikimedia.org/wikipedia/commons/thumb/9/98/Latte_with_winged_tulip_art.jpg/800px-Latte_with_winged_tulip_art.jpg"),
                "Бодрящий латте по узбекски \r\n\r\nЦена: 80 000 сум"
            );
        }



        private async Task SendBlackTeaInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://mpr-shop.ru/upload/medialibrary/afd/qw5akcin1nbghmfnky6hkdygteohj56m.jpg"),
                "Смесь индийского и цейлонского чая с добавлением чабреца\r\n\r\nЦена: 20 000 сум"
            );
        }
        private async Task SendGreenTeaInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://majaro.ru/wa-data/public/photos/68/01/168/168.970.png"),
                "Смесь индийского чая с добавлением ягод, листьев земляники\r\n\r\nЦена: 20 000 сум"
            );
        }
        private async Task SendMilkTeaInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://kivahan.ru/wp-content/uploads/2015/10/molochniy-ulun05-e1444934976743.jpg"),
                "Чай из провинции Юнь-Нань. Тонизирующий и бодрящий чай.\r\n\r\nЦена: 20 000 сум"
            );
        }


        private async Task SendWaterGAZInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://dietology.pro/upload/iblock/fc3/fc3e30d8ff907307ca4bf81504b3f39d.jpeg"),
                "Газированная вода по узбекски\r\n\r\nЦена: 20 000 сум"
            );
        }
        private async Task SendWaterInformationRu()
        {
            await SendDishInformationRu(
                InputFile.FromUri("https://www.zdorovieinfo.ru/wp-content/uploads/2019/06/shutterstock_272582186.jpg"),
                "Вода по узбекски\r\n\r\nЦена: 20 000 сум"
            );
        }


        // Add review and send message
        private async Task AddReviewsAndSendMessageAsyncRu(string score)
        {
            var user = RetrieveUser();

            if (user is not null)
            {
                Review review = new Review()
                {
                    Id = Guid.NewGuid(),
                    Score = score,
                    UserId = user.Id
                };

                await this.reviewService.AddReviewAsync(review);

                var removeKeyboard = new ReplyKeyboardRemove();

                string message = "Оставьте отзыв в виде сообщения или аудиосообщения";

                await botClient.SendTextMessageAsync(ChatId, message, replyMarkup: removeKeyboard);

                menuStack.Push((message, null));
            }
        }


        // Send dish information
        private async Task SendDishInformationRu(InputFile photoUrl, string caption)
        {
            await this.telegramBroker.SendPhotoAsync(
             ChatId,
             photoUrl,
             caption: caption
         );

            var markup = GenerateCountKeyboardMarkupRu();

            string message = "Выберите или введите количество:";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);


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
            try
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

                long userId = 1924521160;

                await client.SendTextMessageAsync(userId, "An error occurred. Please try again later.", cancellationToken: token);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in ErrorHandler: {ex}");
            }
        }


        // Send Messages
        private async Task SendMessageAsync(string message) =>
            await telegramBroker.SendMessageAsync(ChatId, message);


        // Model operations
        private async Task SendMessagesWithMarkupAsync(string message, ReplyKeyboardMarkup markup) =>
            await telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

        private Models.Users.User RetrieveUser()
        {
            var user = this.userService.RetrieveAllUsers().FirstOrDefault(U => U.TelegramId == ChatId);

            return user;
        }

        private Order RetrieveOrderByUserIdAsync(Guid userId) =>
            this.orderService.RetrieveAllOrders().FirstOrDefault(o => o.UserId == userId);

        private async ValueTask<Order> ModifyOrderAsync(Order order) =>
            await this.orderService.ModifyOrderAsync(order);
    }
}

