//===========================
// Copyright (c) Tarteeb LLC
// Order quickly and easily
//===========================

using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using restaurant_bot.Models.Users;

namespace restaurant_bot.Brokers.Storages
{
    public partial class StorageBroker
    {
        public DbSet<User> Users { get; set; }

        public async ValueTask<User> InsertUserAsync(User user) =>
            await InsertAsync(user);
        public async ValueTask<User> UpdateUserAsync(User user) =>
            await UpdateAsync(user);
        public async ValueTask<User> SelectUserByIdAsync(Guid id) =>
            await SelectAsync<User>(id);
        public IQueryable<User> SelectAllUsers() =>
            SelectAll<User>().AsQueryable();
        public ValueTask<User> DeleteUserAsync(User user) =>
            DeleteAsync(user);
    }
}
