//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using restaurant_bot.Models.Reviews;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace restaurant_bot.Brokers.Storages
{
    public partial interface IStorageBroker
    {
        ValueTask<Review> InsertReviewAsync(Review review);
        IQueryable<Review> SelectAllReviews();
        ValueTask<Review> UpdateReviewAsync(Review review);
        ValueTask<Review> SelectReviewByIdAsync(Guid id);
        ValueTask<Review> DeleteReviewAsync(Review review);
    }
}
