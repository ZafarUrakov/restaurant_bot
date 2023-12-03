//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Brokers.Storages;
using restaurant_bot.Services.Foundations.Orders;
using System.Linq;
using System.Threading.Tasks;
using System;
using restaurant_bot.Models.Orders;

namespace restaurant_bot.Services.Foundations.Orders
{
    public partial class OrderService : IOrderService
    {
        private readonly IStorageBroker storageBroker;

        public OrderService(
            IStorageBroker storageBroker)
        {
            this.storageBroker = storageBroker;
        }
        public async ValueTask<Order> AddOrderAsync(Order order) =>
            await this.storageBroker.InsertOrderAsync(order);

        public async ValueTask<Order> RetrieveOrderByIdAsync(Guid orderId) =>
            await this.storageBroker.SelectOrderByIdAsync(orderId);

        public IQueryable<Order> RetrieveAllOrders() =>
            this.storageBroker.SelectAllOrders();

        public async ValueTask<Order> ModifyOrderAsync(Order order) =>
            await this.storageBroker.UpdateOrderAsync(order);

        public async ValueTask<Order> RemoveOrderAsync(Guid orderId)
        {
            Order maybeOrder =
                await this.storageBroker.SelectOrderByIdAsync(orderId);

            return await this.storageBroker.DeleteOrderAsync(maybeOrder);
        }
    }
}
