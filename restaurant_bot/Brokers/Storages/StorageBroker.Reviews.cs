//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using Microsoft.EntityFrameworkCore;
using restaurant_bot.Models.Reviews;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Brokers.Storages
{
    public partial class StorageBroker
    {
        public DbSet<Review> Reviews { get; set; }

        public async ValueTask<Review> InsertReviewAsync(Review review) =>
            await InsertAsync(review);
        public async ValueTask<Review> UpdateReviewAsync(Review review) =>
            await UpdateAsync(review);
        public async ValueTask<Review> SelectReviewByIdAsync(Guid id) =>
            await SelectAsync<Review>(id);
        public IQueryable<Review> SelectAllReviews() =>
            SelectAll<Review>().AsQueryable();
        public ValueTask<Review> DeleteReviewAsync(Review review) =>
            DeleteAsync(review);
    }
}
