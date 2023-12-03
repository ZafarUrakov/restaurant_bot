//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using System.Linq;
using System.Threading.Tasks;
using System;
using restaurant_bot.Models.Orders;

namespace restaurant_bot.Services.Foundations.Orders
{
    public interface IOrderService
    {
        ValueTask<Order> AddOrderAsync(Order order);
        ValueTask<Order> RetrieveOrderByIdAsync(Guid orderId);
        IQueryable<Order> RetrieveAllOrders();
        ValueTask<Order> ModifyOrderAsync(Order order);
        ValueTask<Order> RemoveOrderAsync(Guid orderId);
    }
}
