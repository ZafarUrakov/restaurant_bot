﻿//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Brokers.Telegrams;
using restaurant_bot.Services.Foundations.Users;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private int selectedQuantity;
        private string comment;

        private Message Message { get; set; }
        private long ChatId { get; set; }
        private string Text { get; set; }

        private readonly ITelegramBotClient botClient;
        private readonly ITelegramBroker telegramBroker;
        private readonly IUserService userService;
        private readonly Stack<(string message,
            ReplyKeyboardMarkup markup)> menuStack = new Stack<(string, ReplyKeyboardMarkup)>();

        private Dictionary<string, int> basket = new Dictionary<string, int>();

        private static readonly Dictionary<string?, int> prices = new Dictionary<string?, int>
        {
            { "Бизнес-ланч № 1", 60000 },
            { "Бизнес-ланч № 2", 65000 },
            { "Бизнес-ланч № 3", 68000 }
        };


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

        // Handle TEXT Message
        private async Task HandleTextMessage()
        {
            await HandleStartSectionCommands();

            await HandleLanguageSectionCommands();

            await HandlePhoneNumberSectionCommands();

            await HandleOrderSectionCommands();

            await UpdateUserStatusBasedOnPrice();

            await HandleBranchesCommands();

            await HandleMenuCommand();

            await ProcessBasketAction();

            await HandleDishSelection();

            await HandleCommetMessage();

            switch (Text)
            {
                case "🚖 Оформить заказ":
                    await CreatePlaceOrderMarkup();
                    break;
                case "⬅️ Меню":
                    await HandleBackToMenuCommand();
                    break;
                case "💵 Наличные":
                    await SendReadyOrderMessage();
                    break;
                case "❌ Отменить":
                    await HandleBackCommand();
                    break;
            }

        }

        // Handle comment
        private async Task HandleCommetMessage()
        {
            var user = this.userService
                .RetrieveAllUsers().FirstOrDefault(u => u.TelegramId == ChatId);

            if (user != null)
            {
                var menuStackMessage = menuStack.Peek().message;

                if (menuStackMessage == "Напишите комментарии к заказу")
                {
                    if (Text != "⬅️ Назад" || Text != "⬅️ Меню")
                    {
                        user.Comment = Text;

                        var updatedUser = await this.userService.ModifyUserAsync(user);

                        var markup = await CreatePaymentMarkup();

                        menuStack.Push((menuStackMessage, markup));
                    }
                }
            }
        }

        // Handle all dishes
        private async Task HandleDishSelection()
        {
            switch (Text)
            {

                case "Бизнес-ланч № 1":
                    await SendBusinessLunchNumberOneInformation();
                    break;
                case "Бизнес-ланч № 2":
                    await SendBusinessLunchNumberTwoInformation();
                    break;
                case "Бизнес-ланч № 3":
                    await SendBusinessLunchNumberThreeInformation();
                    break;
            }
        }

        // Backet processes
        private async Task ProcessBasketAction()
        {
            switch (Text)
            {
                case "📥 Корзина":
                    await SendBasketInformation();
                    break;
                case "🔄 Очистить":
                    await RemoveAllDishesFromBasket();
                    break;
                case
                string case1 when case1.StartsWith("❌ "):
                    string separatedPart = case1.Substring(1).TrimStart();
                    await RemoveDishesFromBasketStartingWith(separatedPart);
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
                    await HandleQuantityButtonPress(Text);
                    break;
            }
        }

        // Remove dishes
        private async Task RemoveAllDishesFromBasket()
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

            await SendBasketInformationIfDishesDeleted();

        }

        private async Task RemoveDishesFromBasketStartingWith(string dishForDelete)
        {
            basket.Remove(dishForDelete);

            await SendBasketInformationIfDishesDeleted();
        }

        // Handle start section
        private async Task HandleStartSectionCommands()
        {
            var user = this.userService
                .RetrieveAllUsers().FirstOrDefault(u => u.TelegramId == ChatId);

            if (user is null)
            {
                switch (Text)
                {

                    case "/start":
                        await HandleStartCommand();
                        break;
                }
            }
            else if (Text is "/start")
            {
                await ComeToMainAgain();
            }
            else
            {
                await HandleMainSectionCommands();
            }

        }
        private async Task HandleLanguageSectionCommands()
        {
            var user = this.userService
                .RetrieveAllUsers().FirstOrDefault(u => u.TelegramId == ChatId);

            if (user == null)
            {
                switch (Text)
                {

                    case "🇷🇺 Русский":
                        await HandleRussianLanguage();
                        break;
                    case "🇺🇿 Uzbek":
                        await HandleRussianLanguage();
                        break;
                    case "🇺🇸 English":
                        await HandleRussianLanguage();
                        break;
                }
            }
            else
            {
            }
        }

        // Handle phone number section
        private async Task HandlePhoneNumberSectionCommands()
        {
            var user = this.userService
                .RetrieveAllUsers().FirstOrDefault(u => u.TelegramId == ChatId);

            if (user is null)
            {
                if (IsCommandExpectedInCurrentPhoneNumberSection(Text))
                {
                    if (IsPhoneNumberValid(Text))
                    {
                        await HandleContactWithouShareMessage(Text);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
        private bool IsPhoneNumberValid(string text)
        {
            return text.StartsWith("+") && text.Count(char.IsDigit) > 11;
        }
        private bool IsCommandExpectedInCurrentPhoneNumberSection(string command)
        {
            return IsPhoneNumberValid(command);
        }

        // Handle main section
        private async Task HandleMainSectionCommands()
        {
            if (IsCommandExpectedInCurrentMainSection(Text))
            {
                switch (Text)
                {

                    case "🛍 Заказать":
                        await HandleOrderCommand();
                        break;
                    case "✍️ Оставить отзыв":
                        await HandleOrderCommand();
                        break;
                    case "☎️ Связаться с нами":
                        await HandleOrderCommand();
                        break;
                    case "ℹ️ Информация":
                        await HandleOrderCommand();
                        break;
                    case "⚙️ Настройки":
                        await HandleOrderCommand();
                        break;
                }
            }
            else
            {
                return;
            }
        }
        private bool IsCommandExpectedInCurrentMainSection(string command)
        {
            List<string> expectedCommands = GetExpectedCommandsForCurrentMainSection();
            return expectedCommands.Contains(command);
        }
        private List<string> GetExpectedCommandsForCurrentMainSection()
        {
            return new List<string> { "🛍 Заказать", "✍️ Оставить отзыв",
                "☎️ Связаться с нами", "ℹ️ Информация", "⚙️ Настройки" };
        }

        // Handle other sections...
        private async Task HandleOrderSectionCommands()
        {
            if (Text is not null)
            {
                switch (Text)
                {
                    case "⬅️ Назад" when menuStack.Count > 1:
                        await HandleBackCommand();
                        break;
                    case "🚖 Доставка":
                        await HandleDeliveryCommand();
                        break;
                    case "🏃 Самовывоз":
                        await HandlePickupCommand();
                        break;
                }
            }
            else
            {
                return;
            }
        }

        private async Task HandleMenuCommand()
        {
            switch (Text)
            {
                case "Бизнес-ланчи":
                    await CreateBusinessLunchMarkup();
                    break;
                case "Cамса":
                    await CreateSomsaMarkup();
                    break;
                case "Плов":
                    await CreateOshMarkup();
                    break;
                case "Шашлыки":
                    await CreateKebabsMarkup();
                    break;
                case "Супы":
                    await CreateSoupsMarkup();
                    break;
                case "Салаты":
                    await CreateSaladsMarkup();
                    break;
                case "Чай":
                    await CreateTeaMarkup();
                    break;
                case "Кофе":
                    await CreateCoffeeMarkup();
                    break;
                case "Вода":
                    await CreateWaterMarkup();
                    break;
            }
        }

        private async Task HandleBranchesCommands()
        {
            switch (Text)
            {
                case "Новза":
                case "ЦУМ":
                case "Гидрометцентр":
                case "Сергели":
                case "Кукча":
                    await HandleLocationSelection();
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

        private async Task HandleBackToMenuCommand()
        {
            ReplyKeyboardMarkup markup = CreateMenuMarkup();
            markup.ResizeKeyboard = true;

            string firstMessage = "С чего начнем?";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, firstMessage, markup);
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
            var user = RetrieveUserByChatId();

            user.OrderType = Text;

            await ModifyUserAsync(user);

            ReplyKeyboardMarkup markup = CreateDeliveryMarkup();
            string message = "Куда нужно доставить ваш заказ 🚙?";
            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }

        private async Task HandlePickupCommand()
        {
            var user = RetrieveUserByChatId();

            user.OrderType = Text;

            await ModifyUserAsync(user);

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

                ReplyKeyboardMarkup markup = CreateMainMarkup();
                string firstMessage = $"Отлично, спасибо за регистрацию {expectedUser.FirstName} {expectedUser.LastName}  🥳\n\n";
                string secondMessage = $"Оформим заказ вместе? 😃";

                await SendMessageAsync(ChatId, firstMessage);
                await SendMessagesWithMarkupAsync(secondMessage, markup);

            }
        }

        private async Task HandleContactWithouShareMessage(string contact)
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

                ReplyKeyboardMarkup markup = CreateMainMarkup();
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
                ReplyKeyboardMarkup markup = CreatePickupMarkup();
                await SendMessagesWithMarkupAsync(secondMessage, markup);
            }
        }

        private async Task HandleLocationSelection()
        {
            ReplyKeyboardMarkup markup = CreateMenuMarkup();
            markup.ResizeKeyboard = true;

            string firstMessage = "С чего начнем?";
            string secondMessage = "Продолжим? 😉";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, firstMessage, markup);

            menuStack.Push((secondMessage, markup));
        }


        // Create some murkups
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

        private static ReplyKeyboardMarkup CreateMainMarkup()
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

        private static ReplyKeyboardMarkup CreateMenuMarkup()
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

        private Task<ReplyKeyboardMarkup> CreateBacketMarkup(Dictionary<string, int> dishes)
        {
            var buttons = new List<KeyboardButton[]>();

            foreach (var dish in dishes.Keys)
            {
                buttons.Add(new KeyboardButton[] { new KeyboardButton($"❌ {dish}") });
            }

            buttons.Add(new KeyboardButton[] { new KeyboardButton("⬅️ Назад"), new KeyboardButton("🔄 Очистить") });

            buttons.Add(new KeyboardButton[] { new KeyboardButton("🚖 Оформить заказ") });

            ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(buttons.ToArray())
            {
                ResizeKeyboard = true
            };

            return Task.FromResult(markup);
        }


        //Menu information
        private async Task<ReplyKeyboardMarkup> CreateBusinessLunchMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSomsaMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateOshMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateKebabsMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSoupsMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateSaladsMarkup()
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
            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateTeaMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateCoffeeMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreateWaterMarkup()
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

            await SendMenuInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreatePlaceOrderMarkup()
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

            await SendComentInstruction(markup);

            return markup;
        }
        private async Task<ReplyKeyboardMarkup> CreatePaymentMarkup()
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

            await SendPaymentInstruction(markup);

            return markup;
        }

        private async Task<ReplyKeyboardMarkup> SendReadyOrderMessage()
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

            await SendReadyOrderInstruction(markup);

            return markup;
        }

        // Send ready order information
        private async Task SendReadyOrderInstruction(ReplyKeyboardMarkup markup)
        {
            var user = RetrieveUserByChatId();

            if (user is not null)
            {
                StringBuilder basketInfo = new StringBuilder("Ваш заказ:\n\n");

                foreach (var item in basket)
                {
                    int itemTotal = item.Value * prices[item.Key];
                    basketInfo.AppendLine($"Тип заказа: {user.OrderType}\n" +
                        $"Телефон: {user.PhoneNumber}\n" +
                        $"Способ оплаты: {Text}\n" +
                        $"Коментарий: {user.Comment}\n\n\n" +
                        $"{item.Key}\n{item.Value} x {prices[item.Key]:N0} сум = {itemTotal:N0} сум\n");
                }

                basketInfo.AppendLine($"Сумма: {CalculateTotalPrice():N0} сум");

                await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, basketInfo.ToString(), markup);
            }
            else
            {
                return;
            }
        }

        // Send payment instuction
        private async Task SendPaymentInstruction(ReplyKeyboardMarkup markup)
        {
            string message = "Выберите способ оплаты за Ваш заказ";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));
        }

        // Send coment instruction
        private async Task SendComentInstruction(ReplyKeyboardMarkup markup)
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

        // Send menu instruction
        private async Task SendMenuInstruction(ReplyKeyboardMarkup markup)
        {
            string message = "Нажмите «⏬ Список » для ознакомления с меню или выберите блюдо";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

            menuStack.Push((message, markup));
        }

        // Keyboards markup number of dishes
        private static ReplyKeyboardMarkup GenerateCountKeyboardMarkup()
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
        private int CalculateTotalPrice()
        {
            int total = 0;
            foreach (var item in basket)
            {
                if (prices.TryGetValue(item.Key, out int price))
                {
                    total += price * item.Value;
                }
            }
            return total;
        }

        private async Task SendBasketInformation()
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
                int itemTotal = item.Value * prices[item.Key];
                basketInfo.AppendLine($"{item.Key}\n{item.Value} x {prices[item.Key]:N0} сум = {itemTotal:N0} сум\n");
            }

            basketInfo.AppendLine($"Сумма: {CalculateTotalPrice():N0} сум");

            var markup = await CreateBacketMarkup(basket);

            string message = "*«❌ Наименование »* - удалить одну позицию \r\n " +
                "*«🔄 Очистить »* - полная очистка корзины";

            await this.telegramBroker.SendMessageAsync(ChatId, message);

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, basketInfo.ToString(), markup);

        }

        // Send basket information
        private async Task SendBasketInformationIfDishesDeleted()
        {
            if (basket.Count == 0)
            {
                var updatedMessage = "Ваша корзина пуста";

                int stepsToGoBack = 3;

                for (int i = 0; i < stepsToGoBack; i++)
                {
                    if (menuStack.Count > 1)
                    {
                        menuStack.Pop();
                    }
                    else
                    {
                        break;
                    }
                }

                var previousMenu = menuStack.Peek();

                await SendMessagesWithMarkupAsync(updatedMessage, previousMenu.markup);

                return;
            }


        }

        //Update status
        private async Task UpdateUserStatusBasedOnPrice()
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
        private async Task HandleQuantityButtonPress(string quantity)
        {
            var user = this.userService.RetrieveAllUsers()
                                .FirstOrDefault(u => u.TelegramId == ChatId);

            var selectedDish = user.Status;

            if (string.IsNullOrEmpty(selectedDish))
            {
                await this.telegramBroker.SendMessageAsync(ChatId, "Ошибка: Выберите блюдо сначала.");
                return;
            }

            if (int.TryParse(quantity, out int selectedQuantity))
            {
                await AddToBasket(selectedDish, selectedQuantity);

                selectedDish = null;

                this.selectedQuantity = 0;
            }
            else
            {
                await this.telegramBroker.SendMessageAsync(ChatId,
                    "Некорректное количество. Пожалуйста, выберите количество с клавиатуры.");
            }
        }

        private async Task AddToBasket(string itemName, int quantity)
        {
            if (basket.ContainsKey(itemName))
            {
                basket[itemName] += quantity;
            }
            else
            {
                basket[itemName] = quantity;
            }

            ReplyKeyboardMarkup markup = CreateMenuMarkup();

            markup.ResizeKeyboard = true;

            string message = "Продолжим? 😉";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

        }

        // Send dishes information
        private async Task SendBusinessLunchNumberOneInformation()
        {
            await SendDishInformation(
                InputFile.FromUri("https://media-cdn.tripadvisor.com/media/photo-s/0e/ae/35/29/business-lunch.jpg"),
                "Бизнес-ланч № 1" +
                "Первое блюдо: Мержимек Ezo Gelin Çorbasi 1/2 порции.\r\n" +
                "Второе блюдо: Куриные котлеты с сыром с турецкой лепешкой Екмек.\r\n" +
                "Напиток: Лимонад ягодный 0.4 л.\r\n\r\nЦена: 60 000 сум"
            );
        }

        // Come to menu
        private async Task ComeToMainAgain()
        {
            ReplyKeyboardMarkup markup = CreateMainMarkup();
            string message = $"Продолжим ? 😃";

            await SendMessagesWithMarkupAsync(message, markup);

            menuStack.Push((message, markup));
        }


        // // Send dishes information
        private async Task SendBusinessLunchNumberTwoInformation()
        {
            await SendDishInformation(
                 InputFile.FromUri("https://marhaba.qa/qatarlinks/wp-content/uploads/2020/07/Business-Lunch-Al-Baraha_Easy-Resize.com_.jpg"),
                "Бизнес-ланч № 2" +
                "Первое блюдо: куриный суп 1/2 порции.\r\n" +
                "Второе блюдо: шашлык говяжий с турецкой лепешкой Екмек.\r\n" +
                "Напиток: чай с лимоном.\r\n\r\nЦена: 65 000 сум"
            );
        }
        private async Task SendBusinessLunchNumberThreeInformation()
        {
            await SendDishInformation(
                 InputFile.FromUri("https://mado.az/pics/259/259/product/689/1_1677142202.jpg"),
                "Бизнес-ланч № 3" +
                "Основное блюдо: рыба с овощами 1/2 порция с турецкой лепешкой Екмек.\r\n" +
                "Салат на выбор: греческий 1/2 порция или свежий 1/2 порция.\r\n" +
                "Напиток: чай с лимоном.\r\n\r\nЦена: 68 000 сум"
            );
        }

        // Send dish information
        private async Task SendDishInformation(InputFile photoUrl, string caption)
        {
            await this.telegramBroker.SendPhotoAsync(
             ChatId,
             photoUrl,
             caption: caption
         );

            var markup = GenerateCountKeyboardMarkup();

            string message = "Выберите или введите количество:";

            await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);


        }

        // Send Messages
        private async Task SendMessageAsync(long chatId, string message) =>
            await telegramBroker.SendMessageAsync(ChatId, message);

        private async Task SendMessagesWithMarkupAsync(string message, ReplyKeyboardMarkup markup) =>
            await telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

        private Models.Users.User RetrieveUserByChatId() =>
            this.userService.RetrieveAllUsers().FirstOrDefault(U => U.TelegramId == ChatId);

        private async ValueTask<Models.Users.User> ModifyUserAsync(Models.Users.User user) =>
            await this.userService.ModifyUserAsync(user);

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
