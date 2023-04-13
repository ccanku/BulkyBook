using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class UserController : Controller
    {

        private readonly ApplicationDbContext _db;
        [BindProperty]
        private ApplicationUserVM ApplicationUserVM { get; set; }

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string? userId)
        {

            if(string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == userId);


            if(userFromDb == null)
            {
                return NotFound();
            }

            ApplicationUserVM = new ApplicationUserVM()
            {
                User = userFromDb,
                UserRoleId = _db.UserRoles.ToList().FirstOrDefault(u => u.UserId == userId).RoleId,
                UserCompanyId = userFromDb.CompanyId,
                RoleList = _db.Roles.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),

                CompanyList = _db.Companies.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })
            };

            return View(ApplicationUserVM);
        }

        [HttpPost]
        public IActionResult RoleManagement() 
        {

            return View();
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = _db.ApplicationUsers.Include(u=>u.Company).ToList();

            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach(var user in userList)
            {
                var roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(u=>u.Id == roleId).Name ;
                if(user.Company == null)
                {
                    user.Company = new Company()
                    {
                        Name = ""
                    };
                }
            }

            return Json(new { data = userList });

        }
        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {

            var userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if(userFromDb == null)
            {
                return Json(new {success = false, message = "Error while locking/unlocking."});
            }

            if(userFromDb.LockoutEnd!=null && userFromDb.LockoutEnd > DateTime.Now)
            {
                userFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                userFromDb.LockoutEnd = DateTime.Now.AddYears(1);
            }

            _db.SaveChanges();

            return Json(new {success = true, message = "Operation Successful."});
        }

        #endregion
    }
}
