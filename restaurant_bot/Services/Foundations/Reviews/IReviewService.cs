//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using restaurant_bot.Models.Reviews;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Services.Foundations.Reviews
{
    public interface IReviewService
    {
        ValueTask<Review> AddReviewAsync(Review review);
        ValueTask<Review> RetrieveReviewByIdAsync(Guid reviewId);
        IQueryable<Review> RetrieveAllReviews();
        ValueTask<Review> ModifyReviewAsync(Review review);
        ValueTask<Review> RemoveReviewAsync(Guid reviewId);
    }
}
