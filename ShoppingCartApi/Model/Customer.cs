using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Model
{
    public class Customer
    {
        public string Name { get; set; }
        public CustomerType Type { get; set; } = CustomerType.Standard;
        public string Address { get; set; }
        [EmailAddress]
        public string Email { get; set; }
    }
}
