//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using restaurant_bot.Brokers.Storages;
using restaurant_bot.Models.Dishes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Services.Foundations.Dishes
{
    public partial class DishService : IDishService
    {
        private readonly IStorageBroker storageBroker;

        public DishService(
            IStorageBroker storageBroker)
        {
            this.storageBroker = storageBroker;
        }
        public async ValueTask<Dish> AddDishAsync(Dish dish) =>
            await this.storageBroker.InsertDishAsync(dish);

        public async ValueTask<Dish> RetrieveDishByIdAsync(Guid dishId) =>
            await this.storageBroker.SelectDishByIdAsync(dishId);

        public IQueryable<Dish> RetrieveAllDishs() =>
            this.storageBroker.SelectAllDishs();

        public async ValueTask<Dish> ModifyDishAsync(Dish dish) =>
            await this.storageBroker.UpdateDishAsync(dish);

        public async ValueTask<Dish> RemoveDishAsync(Guid dishId)
        {
            Dish maybeDish =
                await this.storageBroker.SelectDishByIdAsync(dishId);

            return await this.storageBroker.DeleteDishAsync(maybeDish);
        }
    }
}
