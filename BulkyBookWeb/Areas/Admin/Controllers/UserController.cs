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

        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        [BindProperty]
        public ApplicationUserVM ApplicationUserVM { get; set; }

        public UserController(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
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

            //var userFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == userId);,

            var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == userId,includeProperties:"Company");


            if(userFromDb == null)
            {
                return NotFound();
            }

            ApplicationUserVM = new ApplicationUserVM()
            {
                User = userFromDb,
                //UserRoleId = _db.UserRoles.ToList().FirstOrDefault(u => u.UserId == userId).RoleId,

                RoleList = _roleManager.Roles.ToList().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Name
                }),

                CompanyList = _unitOfWork.Company.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })
            };
            ApplicationUserVM.User.Role = _userManager.GetRolesAsync(userFromDb).GetAwaiter().GetResult().FirstOrDefault();
            return View(ApplicationUserVM);
        }

        [HttpPost]
        public IActionResult RoleManagement(int? companyId) 
        {
            var userFromDb = _unitOfWork.ApplicationUser.GetAll(includeProperties:"Company").FirstOrDefault(u => u.Id == ApplicationUserVM.User.Id);

            if(userFromDb == null)
            {
                return NotFound();
            }

            //var oldRoleId = _db.UserRoles.FirstOrDefault(u => u.UserId == userFromDb.Id).RoleId;
            //var oldRoleName = _db.Roles.FirstOrDefault(u => u.Id == oldRoleId).Name;           
            //var newRoleName = _db.Roles.FirstOrDefault(u=>u.Id == ApplicationUserVM.UserRoleId).Name;

            var oldRoleName = _userManager.GetRolesAsync(userFromDb).GetAwaiter().GetResult().FirstOrDefault();
            var newRoleName = ApplicationUserVM.User.Role;
            if (!string.Equals(oldRoleName, newRoleName))
            {
                _userManager.RemoveFromRoleAsync(userFromDb, oldRoleName).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(userFromDb, newRoleName).GetAwaiter().GetResult();
                if (ApplicationUserVM.User.CompanyId != null)
                {
                    userFromDb.CompanyId = ApplicationUserVM.User.CompanyId;
                }
                if (newRoleName != "Company")
                {
                    userFromDb.CompanyId = null;
                }
                TempData["success"] = "User role and company updated successfully.";
                _unitOfWork.ApplicationUser.Update(userFromDb);
                _unitOfWork.Save();
            }
            else
            {
                if(string.Equals(newRoleName,SD.Role_Company) && ApplicationUserVM.User.CompanyId != userFromDb.CompanyId)
                {
                    userFromDb.CompanyId = ApplicationUserVM.User.CompanyId;
                }
                TempData["success"] = "User company updated successfully.";
                _unitOfWork.ApplicationUser.Update(userFromDb);
                _unitOfWork.Save();
            }          
            
            
            
            return RedirectToAction(nameof(Index));

        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> userList = _unitOfWork.ApplicationUser.GetAll(includeProperties:"Company").ToList();

            foreach(var user in userList)   
            {

                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();
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

            var userFromDb = _unitOfWork.ApplicationUser.Get(u => u.Id == id);
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

            _unitOfWork.Save();

            return Json(new {success = true, message = "Operation Successful."});
        }

        #endregion
    }
}
