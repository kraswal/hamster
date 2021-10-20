using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using hamsterModel;
using Microsoft.AspNetCore.Authorization;
using hamster.Data;
using Microsoft.AspNetCore.Identity;
using hamster.Models.Entities;

namespace hamster.Controllers
{
    [Authorize(Policy = "Administrator")]
    public class TariffsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        public TariffsController(UserManager<AppUser> userManager, AppDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public IActionResult Index()
        {
            return View(_db.Tariffs);
        }
    }
}
