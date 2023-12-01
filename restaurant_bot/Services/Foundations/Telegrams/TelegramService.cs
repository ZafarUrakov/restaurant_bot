//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Brokers.Telegrams;
using restaurant_bot.Services.Foundations.Users;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                    .CreateLogger();

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
                case "Бизнес-ланчи":
                    await CreateBusinessLunchMarkup();
                    break;
                case "Комбо":
                    await CreateKomboMarkup();
                    break;
                case "Блюда с рыбой":
                    await CreateDishesWithFithMarkup();
                    break;
                case "Донары":
                    await CreateDonarsMarkup();
                    break;
                case "Шашлыки":
                    await CreateKebabsMarkup();
                    break;
                case "Котлетки":
                    await CreateCutletsMarkup();
                    break;
                case "Бургеры":
                    await CreateBurgersMarkup();
                    break;
                case "Закуски и гарниры":
                    await CreateSnacksAndSideDishesMarkup();
                    break;
                case "Пицца":
                    await CreatePizzaMarkup();
                    break;
                case "Пиде":
                    await CreatePideMarkup();
                    break;
                case "Сэндвичи и Лаваши":
                    await CreateSandwichesAndPitaBreadsMarkup();
                    break;
                case "Хот-доги":
                    await CreateHotDogsMarkup();
                    break;
                case "Супы":
                    await CreateSoupsMarkup();
                    break;
                case "Салаты":
                    await CreateSaladsMarkup();
                    break;
                case "Соусы":
                    await CreateSausesMarkup();
                    break;
                case "Лимонады":
                    await CreateLemonadesMarkup();
                    break;
                case "Милк шейки":
                    await CreateMilkShakesMarkup();
                    break;
                case "Смузи":
                    await CreateSmoothieMarkup();
                    break;
                case "Фреш":
                    await CreateFreshMarkup();
                    break;
                case "Чай":
                    await CreateTeaMarkup();
                    break;
                case "Кофе":
                    await CreateCoffeeMarkup();
                    break;
                case "Напитки":
                    await CreateBevaragesMarkup();
                    break;
                case "Вода":
                    await CreateWaterMarkup();
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
                new[] { new KeyboardButton("Кофе"), new KeyboardButton("Напитки") },
                new[] { new KeyboardButton("Вода"), new KeyboardButton("⬅️ Назад") },
            });
        }

        private async Task<ReplyKeyboardMarkup> CreateBusinessLunchMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Бизнес-ланч N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Бизнес-ланч N-2"),
                    new KeyboardButton("Бизнес-ланч N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateKomboMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Kombo N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Kombo N-2"),
                    new KeyboardButton("Kombo N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateDishesWithFithMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Блюда с рыбой N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Блюда с рыбой N-2"),
                    new KeyboardButton("Блюда с рыбой N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateDonarsMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Донары N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Донары N-2"),
                    new KeyboardButton("Донары N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateKebabsMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Шашлыки N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Шашлыки N-2"),
                    new KeyboardButton("Шашлыки N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateCutletsMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Котлетки N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Котлетки N-2"),
                    new KeyboardButton("Котлетки N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateBurgersMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Бургеры N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Бургеры N-2"),
                    new KeyboardButton("Бургеры N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSnacksAndSideDishesMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Закуски и гарниры N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Закуски и гарниры N-2"),
                    new KeyboardButton("Закуски и гарниры N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreatePizzaMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Пицца N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Пицца N-2"),
                    new KeyboardButton("Пицца N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreatePideMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Пиде N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Пиде N-2"),
                    new KeyboardButton("Пиде N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSandwichesAndPitaBreadsMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Сэндвичи и Лаваши N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Сэндвичи и Лаваши N-2"),
                    new KeyboardButton("Сэндвичи и Лаваши N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateHotDogsMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Хот-доги N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Хот-доги N-2"),
                    new KeyboardButton("Хот-доги N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSoupsMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Супы N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Супы N-2"),
                    new KeyboardButton("Супы N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSaladsMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Салаты N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Салаты N-2"),
                    new KeyboardButton("Салаты N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSausesMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Соусы N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Соусы N-2"),
                    new KeyboardButton("Соусы N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateLemonadesMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Лимонады N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Лимонады N-2"),
                    new KeyboardButton("Лимонады N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateMilkShakesMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Милк шейки N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Милк шейки N-2"),
                    new KeyboardButton("Милк шейки N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSmoothieMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Смузи N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Смузи N-2"),
                    new KeyboardButton("Смузи N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateFreshMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Фреш N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Фреш N-2"),
                    new KeyboardButton("Фреш N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateTeaMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Чай N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Чай N-2"),
                    new KeyboardButton("Чай N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateCoffeeMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Кофе N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Кофе N-2"),
                    new KeyboardButton("Кофе N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateBevaragesMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Напитки N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Напитки N-2"),
                    new KeyboardButton("Напитки N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateWaterMarkup()
        {
            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
                new[]
                {
                    new KeyboardButton("Вода N-1"),
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("Вода N-2"),
                    new KeyboardButton("Вода N-3")
                },
                new KeyboardButton[]
                {
                    new KeyboardButton("⬅️ Назад")
                }
            })
            {
                ResizeKeyboard = true
            };

            string message = "Выберите раздел";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));

            return markup;
        }

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
