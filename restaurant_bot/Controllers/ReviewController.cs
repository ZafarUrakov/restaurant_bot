using Microsoft.AspNetCore.Mvc;
using restaurant_bot.Models.Reviews;
using restaurant_bot.Services.Foundations.Reviews;
using System.Linq;
using System.Runtime.CompilerServices;

namespace restaurant_bot.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService reviewService;

        public ReviewController(IReviewService reviewService)
        {
            this.reviewService = reviewService;
        }

        [HttpGet("ReviewsAll")]
        public ActionResult<IQueryable<Review>> GetAllClients()
        {
            IQueryable<Review> allReviews = this.reviewService.RetrieveAllReviews();

            return Ok(allReviews);
        }
    }
}
