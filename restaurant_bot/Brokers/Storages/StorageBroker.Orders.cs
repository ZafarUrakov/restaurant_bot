//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using Microsoft.EntityFrameworkCore;
using restaurant_bot.Models.Orders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Brokers.Storages
{
    public partial class StorageBroker
    {
        public DbSet<Order> Orders { get; set; }

        public async ValueTask<Order> InsertOrderAsync(Order order) =>
            await InsertAsync(order);
        public async ValueTask<Order> UpdateOrderAsync(Order order) =>
            await UpdateAsync(order);
        public async ValueTask<Order> SelectOrderByIdAsync(Guid id) =>
            await SelectAsync<Order>(id);
        public IQueryable<Order> SelectAllOrders() =>
            SelectAll<Order>().AsQueryable();
        public ValueTask<Order> DeleteOrderAsync(Order order) =>
            DeleteAsync(order);
    }
}
