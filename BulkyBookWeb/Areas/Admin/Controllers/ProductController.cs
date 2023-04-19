using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.JSInterop.Implementation;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> objProductList = _unitOfWork.Product.GetAll().ToList();
            
            return View(objProductList);
        }
        //GET

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll().
                Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });
            
            CategoryList = new MultiSelectList(CategoryList,"Value","Text");

            ProductVM productVM = new()
            {
                CategoryList = CategoryList,
                Product = new Product()
            };
            //ViewBag.CategoryList = CategoryList;
            //ViewData["CategoryList"] = CategoryList;
            if(id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                productVM.Product = _unitOfWork.Product.Get(u => u.Id == id,includeProperties:"ProductImages");
                return View(productVM);
            }
        }
        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj, List<IFormFile>? files)
        {
            if(obj.Product.Price100 > obj.Product.Price50)
            {
                ModelState.AddModelError("Custom Error","Price100+ should not be higher than Price50+");
            }
            if (obj.Product.Price50 > obj.Product.Price)
            {   
                ModelState.AddModelError("Custom Error","Price50+ should not be higher than Price");
            }
            if (obj.Product.Price > obj.Product.ListPrice)
            {
                ModelState.AddModelError("Custom Error", "Price should not be higher than List Price");
            }
            if (ModelState.IsValid)
            {   
                
                if(obj.Product.ProductCategories == null)
                {
                    obj.Product.ProductCategories = new List<ProductCategory>();
                }

                if (obj.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(obj.Product);
                    _unitOfWork.Save();

                    foreach (var id in obj.CategoryIDs)
                    {
                        

                        ProductCategory productCategory = new ProductCategory()
                        {
                            CategoryId = id,
                            ProductId = obj.Product.Id
                        };
                        obj.Product.ProductCategories.Add(productCategory);
                        _unitOfWork.ProductCategory.Add(productCategory);
                        _unitOfWork.Save();
                    }
                    
                    
                }
                else
                {
                    var oldCategories = _unitOfWork.ProductCategory.GetAll(u => u.ProductId == obj.Product.Id);
                    
                    foreach(var oldCategory in oldCategories)
                    {
                        if (obj.CategoryIDs.Contains(oldCategory.CategoryId))
                        {
                            _unitOfWork.ProductCategory.Remove(oldCategory);
                        }
                        
                    }

                    foreach(var id in obj.CategoryIDs)
                    {
                        if(oldCategories.FirstOrDefault(u=>u.CategoryId == id)==null)
                        {
                            var productCategory = new ProductCategory()
                            {
                                CategoryId = id,
                                ProductId = obj.Product.Id
                            };
                            _unitOfWork.ProductCategory.Add(productCategory);
                        }
                    }

                    _unitOfWork.Product.Update(obj.Product);
                }
                _unitOfWork.Save();

                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (files != null)
                {   

                    foreach(IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string ProductPath = @"images\products\product-" + obj.Product.Id;
                        string FinalPath = Path.Combine(wwwRootPath, ProductPath);
                        if (!Directory.Exists(FinalPath))
                        {
                            Directory.CreateDirectory(FinalPath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(FinalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new ProductImage()
                        {
                            ImageUrl = @"\" + ProductPath + @"\" + fileName,
                            ProductId = obj.Product.Id

                        };

                        if(obj.Product.ProductImages == null)
                        {
                            obj.Product.ProductImages = new List<ProductImage>();
                        }
                        obj.Product.ProductImages.Add(productImage);

                    }
                    _unitOfWork.Product.Update(obj.Product);
                    _unitOfWork.Save();
                }

                TempData["success"] = "Product created or updated successfully";
                return RedirectToAction("Index");
            }
            IEnumerable<SelectListItem> CategoryList = _unitOfWork.Category.GetAll().
                 Select(u => new SelectListItem
                 {
                     Text = u.Name,
                     Value = u.Id.ToString()
                 });

            ProductVM productVM = new()
            {
                CategoryList = CategoryList,
                Product = new Product()
            };
            return View(obj);

        }
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var productFromDb = _unitOfWork.Product.Get(u => u.Id == id);
            //var categoryFromDbFirst = _db.Categories.FirstOrDefault(u=>u.Id == id);
            //var categoryFromSingle = _db.Categories.SingleOrDefault(u=>u.Id == id);
            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Product obj)
        {
            if (obj.Price100 > obj.Price50)
            {
                ModelState.AddModelError("Custom Error", "Price100+ should not be higher than Price50+");
            }
            if (obj.Price50 > obj.Price)
            {
                ModelState.AddModelError("Custom Error", "Price50+ should not be higher than Price");
            }
            if (obj.Price > obj.ListPrice)
            {
                ModelState.AddModelError("Custom Error", "Price should not be higher than List Price");
            }
            if (ModelState.IsValid)
            {
                _unitOfWork.Product.Update(obj);
                _unitOfWork.Save();
                TempData["success"] = "Category created successfully";
                return RedirectToAction("Index");
            }

            return View(obj);

        }
       

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePOST(int? id)
        {
            var obj = _unitOfWork.Product.Get(u => u.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            _unitOfWork.Product.Remove(obj);
            _unitOfWork.Save();
            return RedirectToAction("Index");
        }

        public IActionResult DeleteImage(int imageId)
        {
            var imageToBeDeleted = _unitOfWork.ProductImage.Get(u => u.Id == imageId);
            int productId = imageToBeDeleted.ProductId;
            if (imageToBeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageToBeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToBeDeleted.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                _unitOfWork.ProductImage.Remove(imageToBeDeleted);
                _unitOfWork.Save();
                TempData["success"] = "Image succesfully deleted.";
            }
            return RedirectToAction(nameof(Upsert),new {id = productId});
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll().ToList();
            return Json(new {data = objProductList});
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unitOfWork.Product.Get(u=>u.Id == id);
            if(productToBeDeleted == null)
            {
                return Json(new {success = "false", message = "Error while deleting."});    
            }
            string productPath = @"images\products\product" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath,productPath);
            //var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));
            //if (System.IO.File.Exists(oldImagePath))
            //{
            //    System.IO.File.Delete(oldImagePath);
            //}

            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach(var FilePath in  filePaths)
                {
                    System.IO.File.Delete(FilePath);
                }
                Directory.Delete(finalPath, true);
            }

            _unitOfWork.Product.Remove(productToBeDeleted);
            _unitOfWork.Save();
            return Json(new {success="true",message = "Product succesfully deleted."});
        }

       
        #endregion
    }
}