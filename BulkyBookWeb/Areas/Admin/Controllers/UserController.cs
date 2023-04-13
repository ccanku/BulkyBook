using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;
        [BindProperty]
        public ApplicationUserVM ApplicationUserVM { get; set; }

        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
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
            var userFromDb = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == ApplicationUserVM.User.Id);

            if(userFromDb == null)
            {
                return NotFound();
            }
            
            var oldRoleId = _db.UserRoles.FirstOrDefault(u => u.UserId == userFromDb.Id).RoleId;
            var oldRoleName = _db.Roles.FirstOrDefault(u => u.Id == oldRoleId).Name;           
            var newRoleName = _db.Roles.FirstOrDefault(u=>u.Id == ApplicationUserVM.UserRoleId).Name;
            if (!string.Equals(oldRoleName, newRoleName))
            {
                _userManager.RemoveFromRoleAsync(userFromDb, oldRoleName).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userFromDb, newRoleName).GetAwaiter().GetResult();
            }           
            if (ApplicationUserVM.UserCompanyId != null)
            {
                userFromDb.CompanyId = ApplicationUserVM.UserCompanyId;
            }
            if(newRoleName != "Company")
            {
                userFromDb.CompanyId = null;
            }
            _db.Update(userFromDb);
            _db.SaveChanges();
            TempData["success"] = "User role and company updated successfully.";
            return RedirectToAction(nameof(Index));

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
