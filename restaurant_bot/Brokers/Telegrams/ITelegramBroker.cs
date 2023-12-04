//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace restaurant_bot.Brokers.Telegrams
{
    public interface ITelegramBroker
    {
        ValueTask<Message> SendMessageWithMarkUpAsync(
            long telegramId, string text, ReplyKeyboardMarkup replyMarkup);
        ValueTask<Message> SendMessageAsync(long telegramId, string text);
        ValueTask DeleteMessageAsync(long telegramId, int messageId);
        ValueTask SendPhotoAsync(long telegramId, InputFile photo, string caption);
    }
}
