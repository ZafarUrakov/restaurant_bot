//=================================
// Copyright (c) Tarteeb LLC.
// Powering True Leadership
//=================================

using restaurant_bot.Models.Users;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Services.Foundations.Users
{
    public interface IUserService
    {
        ValueTask<User> AddUserAsync(User user);
        ValueTask<User> RetrieveUserByIdAsync(Guid userId);
        IQueryable<User> RetrieveAllUsers();
        ValueTask<User> ModifyUserAsync(User user);
        ValueTask<User> RemoveUserAsync(Guid userId);
    }
}
