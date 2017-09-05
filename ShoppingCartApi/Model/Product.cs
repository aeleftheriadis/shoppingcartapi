using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Model
{
    public class Product
    {
        [Key]
        public string Code { get; set; }
        public string Description { get; set; }
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}
