//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Models.Orders;
using System;

namespace restaurant_bot.Models.Dishes
{
    public class Dish
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public Guid OrderId { get; set; }
        public Order order { get; set; }
    }
}
