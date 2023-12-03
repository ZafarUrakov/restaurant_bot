//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Models.Orders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Brokers.Storages
{
    public partial interface IStorageBroker
    {
        ValueTask<Order> InsertOrderAsync(Order order);
        IQueryable<Order> SelectAllOrders();
        ValueTask<Order> UpdateOrderAsync(Order order);
        ValueTask<Order> SelectOrderByIdAsync(Guid id);
        ValueTask<Order> DeleteOrderAsync(Order order);
    }
}
