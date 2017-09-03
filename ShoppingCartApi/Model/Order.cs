using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Model
{
    public class Order
    {
        public Guid Id { get; set; }
        public string Customer { get; set; }
        public DateTime Date { get; set; }
        public string Address { get; set; }
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}
