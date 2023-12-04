using Microsoft.AspNetCore.Mvc;
using restaurant_bot.Models.Dishes;
using restaurant_bot.Services.Foundations.Dishes;
using System.Linq;

namespace restaurant_bot.Controllers
{
    public class DishController : Controller
    {
        private readonly IDishService dishService;

        public DishController(IDishService dishService)
        {
            this.dishService = dishService;
        }

        [HttpGet("DishesAll")]
        public ActionResult<IQueryable<Dish>> GetAllClients()
        {
            IQueryable<Dish> allDishs = this.dishService.RetrieveAllDishs();

            return Ok(allDishs);
        }
    }
}
