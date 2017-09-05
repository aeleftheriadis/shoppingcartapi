using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Infastructure
{
    public class ShoppingCartContextSeed
    {
        public static async Task SeedAsync(IApplicationBuilder applicationBuilder, IHostingEnvironment env, ILoggerFactory loggerFactory, int? retry = 0)
        {
            var log = loggerFactory.CreateLogger("shopping card seed");

            var context = (ShoppingCartContext)applicationBuilder
                .ApplicationServices.GetService(typeof(ShoppingCartContext));

            //context.Database.Migrate();

            if (!context.Customer.Any())
            {
                context.Customer.Add(new Model.Customer()
                {
                    Address = "Test Address",
                    Email = "test@local",
                    Name = "Test Name",
                    Type = Model.CustomerType.Standard
                });
                await context.SaveChangesAsync();
            }

            if (!context.Order.Any() && !context.OrderLine.Any())
            {
                var orderId = Guid.NewGuid();
                context.Order.Add(new Model.Order()
                {
                    Address = "Test Address",
                    Amount = 50.5M,
                    Customer = "Test Name",
                    Date = DateTime.Now.AddDays(-5),
                    Id = orderId
                });

                await context.SaveChangesAsync();

                context.OrderLine.Add(new Model.OrderLine()
                {
                    OrderId = orderId,
                    Products = new List<Model.Product>()
                    {
                       new Model.Product()
                       {
                           Code = "000-000-000",
                           Description = "Test Description 1",
                           UnitPrice = 15.0M
                       },
                       new Model.Product()
                       {
                           Code = "000-000-001",
                           Description = "Test Description 2",
                           UnitPrice = 35.5M
                       }
                    }
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
