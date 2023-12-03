//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using System.Linq;
using System.Threading.Tasks;
using System;
using restaurant_bot.Models.Dishes;

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
