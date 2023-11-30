//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

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
        private Stack<ReplyKeyboardMarkup> menuStack = new Stack<ReplyKeyboardMarkup>();
        public TelegramService(
            ITelegramBroker telegramBroker,
            IUserService userService)
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
                    if (Text == "⬅️ Назад" && menuStack.Count > 1)
                    {
                        menuStack.Pop();

                        ReplyKeyboardMarkup previousMenu = menuStack.Peek();

                        await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, "Возвращение назад...", previousMenu);

                    }
                    else if (Text is "/start")
                    {
                        string greetings = "Здравствуйте! Давайте для начала выберем язык обслуживания!\r\n\r\n" +
                            "Keling, avvaliga xizmat ko’rsatish tilini tanlab olaylik.\r\n\r\n" +
                            "Hi! Let's first we choose language of serving!";

                        ReplyKeyboardMarkup markup = new ReplyKeyboardMarkup(new KeyboardButton[]
                        {
                            new KeyboardButton("🇺🇿 Uzbek"),
                            new KeyboardButton("🇷🇺 Русский"),
                            new KeyboardButton("🇺🇸 English")
                        });
                        markup.ResizeKeyboard = true;

                        await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, greetings, markup);

                        menuStack.Clear();
                        menuStack.Push(markup);
                    }
                    else if (Text is "🇷🇺 Русский")
                    {
                        string greetings = "Добро пожаловать в Tarteeb restaurant!";
                        string promptForPhoneNumber = "📱 Какой у Вас номер? Отправьте ваш номер телефона.\r\n\r\n" +
                            "Чтобы отправить номер нажмите на кнопку \"📱 Отправить мой номер\", или \r\n" +
                            "Отправьте номер в формате: +998 ** *** ****";

                        ReplyKeyboardMarkup markup = new 
                            ReplyKeyboardMarkup(KeyboardButton.WithRequestContact("📞 Поделиться контактом📞"));
                        markup.ResizeKeyboard = true;

                        await this.telegramBroker.SendMessageAsync(ChatId, greetings);
                        await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, promptForPhoneNumber, markup);

                        menuStack.Push(markup);
                    }
                    else if (Text == "🛍 Заказать")
                    {
                        ReplyKeyboardMarkup markup =
                            new ReplyKeyboardMarkup
                                    (new KeyboardButton[] {
                                new KeyboardButton("")});

                        markup.Keyboard = new KeyboardButton[][]
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
                         };

                        markup.ResizeKeyboard = true;

                        string message = $"Заберите свой заказ самостоятельно или выберите доставку";

                        await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, message, markup);

                        menuStack.Push(markup);
                    }
                }
                else if (update.Message.Contact.PhoneNumber is not null)
                {
                    Models.Users.User user = new Models.Users.User
                    {
                        Id = Guid.NewGuid(),
                        TelegramId = ChatId,
                        Status = "Поделиться контактом",
                        FirstName = Message.Chat.FirstName,
                        LastName = Message.Chat.LastName,
                        PhoneNumber = update.Message.Contact.PhoneNumber,
                    };

                    Models.Users.User expectedUser = await this.userService.AddUserAsync(user);

                    ReplyKeyboardMarkup markup =
                        new ReplyKeyboardMarkup
                                (new KeyboardButton[] {
                                new KeyboardButton("")});

                    markup.Keyboard = new KeyboardButton[][]
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
                     };

                    markup.ResizeKeyboard = true;

                    string welcome = $"Отлично, " +
                        $"{expectedUser.FirstName} {expectedUser.LastName} 🥳\n\n" +
                        $"Оформим заказ вместе? 😃";

                    await this.telegramBroker.SendMessageWithMarkUpAsync(ChatId, welcome, markup);

                    menuStack.Push(markup);
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

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            return;
        }
    }
}
