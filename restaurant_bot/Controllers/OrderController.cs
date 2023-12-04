using Microsoft.AspNetCore.Mvc;
using restaurant_bot.Models.Orders;
using restaurant_bot.Services.Foundations.Orders;
using System.Linq;

namespace restaurant_bot.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService orderService;

        public OrderController(IOrderService orderService)
        {
            this.orderService = orderService;
        }

        [HttpGet("OrdersAll")]
        public ActionResult<IQueryable<Order>> GetAllClients()
        {
            IQueryable<Order> allOrders = this.orderService.RetrieveAllOrders();

            return Ok(allOrders);
        }
    }
}
