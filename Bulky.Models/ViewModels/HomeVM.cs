using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.ViewModels
{
    public class HomeVM
    {
        public List<Product> ProductList { get; set; }
        public IEnumerable<Category> CategoryList { get; set; }
    }
}
