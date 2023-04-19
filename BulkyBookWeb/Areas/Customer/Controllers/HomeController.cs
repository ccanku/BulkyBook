using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Bulky.Models;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Bulky.Utility;
using Bulky.Models.ViewModels;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index(int? categoryId)
        {
            List<Product> ProductList = new List<Product>();
            IEnumerable<Category> CategoryList = _unitOfWork.Category.GetAll(includeProperties:"ProductCategories");
            if (categoryId != null)
            {
               var categoryFromDb = _unitOfWork.Category.Get(u => u.Id == categoryId,includeProperties:"ProductCategories");
               foreach(var productcategory in categoryFromDb.ProductCategories)
                {
                    var productFromDb = _unitOfWork.Product.Get(u => u.Id == productcategory.ProductId,includeProperties:"ProductImages,ProductCategories");
                    ProductList.Add(productFromDb);
                }
            }
            else
            {
                ProductList = _unitOfWork.Product
                .GetAll(includeProperties: "ProductImages,ProductCategories").ToList();
            }
            
            HomeVM HomeVM = new HomeVM()
            {
                ProductList = ProductList,
                CategoryList = CategoryList
            };

            return View(HomeVM);
        }
        
        public IActionResult Details(int productId)
        {
            ShoppingCart cart = new()
            {
                Product = _unitOfWork.Product.Get(u => u.Id == productId, includeProperties: "ProductImages"),
                Count = 1,
                ProductId = productId
        };
            cart.Product.ProductCategories = _unitOfWork.ProductCategory.GetAll(u=>u.ProductId==cart.ProductId,includeProperties: "Category").ToList();
            return View(cart);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart cart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            cart.ApplicationUserId = userId;

            ShoppingCart cartFromDb = _unitOfWork.ShoppingCart.Get(u=>u.ApplicationUserId == userId
                && u.ProductId == cart.ProductId);
            if(cartFromDb != null)
            {
                cartFromDb.Count += cart.Count;
                _unitOfWork.ShoppingCart.Update(cartFromDb);
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(cart);
                
            }
            _unitOfWork.Save();
            HttpContext.Session.SetInt32(SD.SessionCart,
                    _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId).Count());
            TempData["success"] = "Cart updated successfully";
           
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}