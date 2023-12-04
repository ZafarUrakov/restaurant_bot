//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using restaurant_bot.Models.Dishes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Services.Foundations.Dishes
{
    public interface IDishService
    {
        ValueTask<Dish> AddDishAsync(Dish dish);
        ValueTask<Dish> RetrieveDishByIdAsync(Guid dishId);
        IQueryable<Dish> RetrieveAllDishs();
        ValueTask<Dish> ModifyDishAsync(Dish dish);
        ValueTask<Dish> RemoveDishAsync(Guid dishId);
    }
}
