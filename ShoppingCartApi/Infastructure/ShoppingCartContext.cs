using Microsoft.EntityFrameworkCore;
using ShoppingCartApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Infastructure
{
    public class ShoppingCartContext : DbContext
    {
        public ShoppingCartContext(DbContextOptions<ShoppingCartContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customer { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<OrderLine> OrderLine { get; set; }
    }
}
