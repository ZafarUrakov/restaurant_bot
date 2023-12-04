//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Models.Orders;
using restaurant_bot.Models.Reviews;
using System;
using System.Collections.Generic;

namespace restaurant_bot.Models.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public long TelegramId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Location { get; set; } = string.Empty;

        public List<Review> Reviews { get; set; }
        public List<Order> Orders { get; set; }
    }
}
