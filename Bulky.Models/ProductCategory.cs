using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models
{
    public class ProductCategory
    {
        public int id { get;set; }

        [Required]
        public int CategoryId { get;set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }

        [Required]
        public int ProductId { get;set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }
    }
}
