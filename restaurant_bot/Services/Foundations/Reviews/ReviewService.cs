//===========================
// Copyright (c) Tarteeb LLC
// Dish quickly and easily
//===========================

using restaurant_bot.Brokers.Storages;
using restaurant_bot.Models.Reviews;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace restaurant_bot.Services.Foundations.Reviews
{
    public class ReviewService : IReviewService
    {
        private readonly IStorageBroker storageBroker;

        public ReviewService(
            IStorageBroker storageBroker)
        {
            this.storageBroker = storageBroker;
        }
        public async ValueTask<Review> AddReviewAsync(Review review) =>
            await this.storageBroker.InsertReviewAsync(review);

        public async ValueTask<Review> RetrieveReviewByIdAsync(Guid reviewId) =>
            await this.storageBroker.SelectReviewByIdAsync(reviewId);

        public IQueryable<Review> RetrieveAllReviews() =>
            this.storageBroker.SelectAllReviews();

        public async ValueTask<Review> ModifyReviewAsync(Review review) =>
            await this.storageBroker.UpdateReviewAsync(review);

        public async ValueTask<Review> RemoveReviewAsync(Guid reviewId)
        {
            Review maybeReview =
                await this.storageBroker.SelectReviewByIdAsync(reviewId);

            return await this.storageBroker.DeleteReviewAsync(maybeReview);
        }
    }
}
