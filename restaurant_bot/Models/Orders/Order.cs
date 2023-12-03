//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Models.Dishes;
using restaurant_bot.Models.Users;
using System;
using System.Collections.Generic;

namespace restaurant_bot.Models.Orders
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderType { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        public List<Dish> Dishes { get; set; } = new List<Dish>();
        public decimal TotalAmount { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
