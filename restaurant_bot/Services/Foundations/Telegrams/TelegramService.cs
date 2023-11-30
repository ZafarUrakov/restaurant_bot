using restaurant_bot.Brokers.Telegrams;
using restaurant_bot.Models.Users;
using restaurant_bot.Services.Foundations.Users;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace restaurant_bot.Services.Foundations.Telegrams
{
    public class TelegramService : ITelegramService
    {
        private Message Message { get; set; }
        private long ChatId { get; set; }
        private string Text { get; set; }
        private readonly ITelegramBotClient botClient;
        private readonly ITelegramBroker telegramBroker;
        private readonly IUserService userService;
        private readonly Stack<(string message, 
            ReplyKeyboardMarkup markup)> menuStack = new Stack<(string, ReplyKeyboardMarkup)>();

        public TelegramService(ITelegramBroker telegramBroker, IUserService userService)
        {
            string token = "6980223449:AAF69OLZRY9ICfTwrt6cWjL-cdVSTXHEx4c";
            this.botClient = new TelegramBotClient(token);
            this.telegramBroker = telegramBroker;
            this.userService = userService;
        }

        public void StartListening()
        {
            botClient.StartReceiving(MessageHandler, ErrorHandler);
        }

        private async Task MessageHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            if (update.Message is not null)
            {
                Message = update.Message;
                ChatId = update.Message.Chat.Id;
                Text = update.Message.Text;

                if (Text is not null)
                {
                    await HandleTextMessage();
                }
                else if (update.Message.Contact?.PhoneNumber is not null)
                {
                    await HandleContactMessage(update.Message.Contact);
                }
                else if (update.Message.Location?.Latitude is not null)
                {
                    await HandleLocationMessage(update.Message.Location);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                return;
            }
        }

        private async Task HandleTextMessage()
        {
            switch (Text)
            {
                case "/start":
                    await HandleStartCommand();
                    break;
                case "⬅️ Назад" when menuStack.Count > 1:
                    await HandleBackCommand();
                    break;
                case "🇷🇺 Русский":
                    await HandleRussianLanguage();
                    break;
                case "🛍 Заказать":
                    await HandleOrderCommand();
                    break;
                case "🚖 Доставка":
                    await HandleDeliveryCommand();
                    break;
                case "🏃 Самовывоз":
                    await HandlePickupCommand();
                    break;
                case "Новза":
                case "ЦУМ":
                case "Гидрометцентр":
                case "Сергели":
                case "Кукча":
                    await HandleLocationSelection(Text);
                    break;
            }
        }

        private async Task HandleStartCommand()
        {
            string greetings = "Здравствуйте! Давайте для начала выберем язык обслуживания!\r\n\r\n" +
                               "Keling, avvaliga xizmat ko’rsatish tilini tanlab olaylik.\r\n\r\n" +
                               "Hi! Let's first we choose language of serving!";

            ReplyKeyboardMarkup markup = CreateLanguageMarkup();
            await SendMessagesWithMarkupAsync(greetings, markup);

            menuStack.Clear();
        }

        private async Task HandleBackCommand()
        {
            menuStack.Pop();
            var previousMenu = menuStack.Peek();
            await SendMessagesWithMarkupAsync(previousMenu.message, previousMenu.markup);
        }

        private async Task HandleRussianLanguage()
        {
            string greetings = "Добро пожаловать в Tarteeb restaurant!";
            string promptForPhoneNumber = "📱 Какой у Вас номер? Отправьте ваш номер телефона.\r\n\r\n" +
                                         "Чтобы отправить номер нажмите на кнопку \"📱 Отправить мой номер\", или \r\n" +
                                         "Отправьте номер в формате: +998 ** *** ****";

            ReplyKeyboardMarkup markup =
                new ReplyKeyboardMarkup(KeyboardButton.WithRequestContact("📞 Поделиться контактом 📞"));
            markup.ResizeKeyboard = true;

            await SendMessageAsync(ChatId, greetings);
            await SendMessagesWithMarkupAsync(promptForPhoneNumber, markup);
        }

        private async Task HandleOrderCommand()
        {
            ReplyKeyboardMarkup markup = CreateOrderMarkup();
            string message = $"Заберите свой заказ самостоятельно или выберите доставку";
            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandleDeliveryCommand()
        {
            ReplyKeyboardMarkup markup = CreateDeliveryMarkup();
            string message = "Куда нужно доставить ваш заказ 🚙?";
            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandlePickupCommand()
        {
            ReplyKeyboardMarkup markup = CreatePickupMarkup();
            string message = "Где вы находитесь 👀?\r\nЕсли вы отправите локацию 📍, мы определим ближайший к вам филиал";
            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandleContactMessage(Contact contact)
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

                ReplyKeyboardMarkup markup = CreateWelcomeMarkup();
                string firstMessage = $"Отлично, спасибо за регистрацию {expectedUser.FirstName} {expectedUser.LastName}  🥳\n\n";
                string secondMessage = $"Оформим заказ вместе? 😃";

                await SendMessageAsync(ChatId, firstMessage);
                await SendMessagesWithMarkupAsync(secondMessage, markup);

                menuStack.Push((secondMessage, markup));
            }
        }

        private async Task HandleLocationMessage(Location location)
        {
            if (location?.Latitude is not null)
            {
                string secondMessage = $"Рядом с вами есть филиалы 📍";
                ReplyKeyboardMarkup markup = CreateLocationMarkup();
                await SendMessagesWithMarkupAsync(secondMessage, markup);
            }
        }

        private async Task HandleLocationSelection(string location)
        {
            ReplyKeyboardMarkup markup = CreateMenuMarkup();
            markup.ResizeKeyboard = true;

            string message = "С чего начнем?";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));
        }

        private async Task SendMessageAsync(long chatId, string message) =>
            await telegramBroker.SendMessageAsync(ChatId, message);

        private async Task SendMessagesWithMarkupAsync(string message, ReplyKeyboardMarkup markup) =>
            await telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

        private static ReplyKeyboardMarkup CreateLanguageMarkup()
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

        private static ReplyKeyboardMarkup CreateOrderMarkup()
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

        private static ReplyKeyboardMarkup CreateDeliveryMarkup()
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

        private static ReplyKeyboardMarkup CreatePickupMarkup()
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

        private static ReplyKeyboardMarkup CreateWelcomeMarkup()
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

        private static ReplyKeyboardMarkup CreateLocationMarkup()
        {
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new KeyboardButton[]
                {
                    new KeyboardButton("ЦУМ")
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

        private static ReplyKeyboardMarkup CreateMenuMarkup()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📥 Корзина"), new KeyboardButton("🚖 Оформить заказ") },
                new[] { new KeyboardButton("Бизнес-ланчи"), new KeyboardButton("Комбо") },
                new[] { new KeyboardButton("Блюда с рыбой"), new KeyboardButton("Донары") },
                new[] { new KeyboardButton("Шашлыки"), new KeyboardButton("Котлетки") },
                new[] { new KeyboardButton("Бургеры"), new KeyboardButton("Закуски и гарниры") },
                new[] { new KeyboardButton("Пицца"), new KeyboardButton("Пиде") },
                new[] { new KeyboardButton("Сэндвичи и Лаваши"), new KeyboardButton("Хот-доги") },
                new[] { new KeyboardButton("Супы"), new KeyboardButton("Салаты") },
                new[] { new KeyboardButton("Соусы"), new KeyboardButton("Лимонады") },
                new[] { new KeyboardButton("Милк шейки"), new KeyboardButton("Смузи") },
                new[] { new KeyboardButton("Фреш"), new KeyboardButton("Чай") },
                new[] { new KeyboardButton("☕️ Кофе"), new KeyboardButton("Напитки") },
                new[] { new KeyboardButton("☕️ Вода"), new KeyboardButton("⬅️ Назад") },
            });
        }


        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            return;
        }
    }
}
