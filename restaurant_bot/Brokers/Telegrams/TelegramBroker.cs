//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace restaurant_bot.Brokers.Telegrams
{
    public class TelegramBroker : ITelegramBroker
    {
        private readonly ITelegramBotClient botClient;
        public TelegramBroker(ITelegramBotClient botClient)
        {
            this.botClient = botClient;
        }

        public async ValueTask<Message> SendMessageWithMarkUpAsync(
            long telegramId, string text, ReplyKeyboardMarkup replyMarkup) =>
            await this.botClient.SendTextMessageAsync(chatId: telegramId, text: text, replyMarkup: replyMarkup);

        public async ValueTask<Message> SendMessageAsync(long telegramId, string text) =>
            await this.botClient.SendTextMessageAsync(chatId: telegramId, text: text);

        public async ValueTask DeleteMessageAsync(long telegramId, int messageId) =>
            await this.botClient.DeleteMessageAsync(telegramId, messageId);

        public async ValueTask SendPhotoAsync(long telegramId, InputFile photo, string caption) =>
            await this.botClient.SendPhotoAsync(chatId: telegramId, photo: photo, caption: caption);

    }
}
