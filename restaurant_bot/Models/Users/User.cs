//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using System;

namespace restaurant_bot.Models.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
    }
}
