using Microsoft.AspNetCore.Mvc;
using restaurant_bot.Models.Users;
using restaurant_bot.Services.Foundations.Users;
using System.Linq;

namespace restaurant_bot.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService userService;

        public UserController(IUserService userService)
        {
            this.userService = userService;
        }

        [HttpGet("UsersAll")]
        public ActionResult<IQueryable<User>> GetAllClients()
        {
            IQueryable<User> allUsers = this.userService.RetrieveAllUsers();

            return Ok(allUsers);
        }
    }
}
