//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using restaurant_bot.Models.Dishes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Brokers.Storages
{
    public partial interface IStorageBroker
    {
        ValueTask<Dish> InsertDishAsync(Dish dish);
        IQueryable<Dish> SelectAllDishs();
        ValueTask<Dish> UpdateDishAsync(Dish dish);
        ValueTask<Dish> SelectDishByIdAsync(Guid id);
        ValueTask<Dish> DeleteDishAsync(Dish dish);
    }
}
