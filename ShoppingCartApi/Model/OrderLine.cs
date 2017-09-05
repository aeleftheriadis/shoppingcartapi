using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Model
{
    public class OrderLine
    {
        [Key]
        public Guid OrderId { get; set; }
        public List<Product> Products { get; set; }
    }
}
