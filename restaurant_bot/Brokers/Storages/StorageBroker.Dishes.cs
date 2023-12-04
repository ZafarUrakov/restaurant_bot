//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using Microsoft.EntityFrameworkCore;
using restaurant_bot.Models.Dishes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Brokers.Storages
{
    public partial class StorageBroker
    {
        public DbSet<Dish> Dishs { get; set; }

        public async ValueTask<Dish> InsertDishAsync(Dish dish) =>
            await InsertAsync(dish);
        public async ValueTask<Dish> UpdateDishAsync(Dish dish) =>
            await UpdateAsync(dish);
        public async ValueTask<Dish> SelectDishByIdAsync(Guid id) =>
            await SelectAsync<Dish>(id);
        public IQueryable<Dish> SelectAllDishs() =>
            SelectAll<Dish>().AsQueryable();
        public ValueTask<Dish> DeleteDishAsync(Dish dish) =>
            DeleteAsync(dish);
    }
}
