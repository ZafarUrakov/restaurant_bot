//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using restaurant_bot.Models.Users;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Brokers.Storages
{
    public partial interface IStorageBroker
    {
        ValueTask<User> InsertUserAsync(User user);
        IQueryable<User> SelectAllUsers();
        ValueTask<User> UpdateUserAsync(User user);
        ValueTask<User> SelectUserByIdAsync(Guid id);
        ValueTask<User> DeleteUserAsync(User user);
    }
}
