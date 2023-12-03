//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using restaurant_bot.Models.Users;
using System;
using System.Reflection.Metadata.Ecma335;

namespace restaurant_bot.Models.Reviews
{
    public class Review
    {
        public Guid Id { get; set; }
        public string Score { get; set; }
        public string Message { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
