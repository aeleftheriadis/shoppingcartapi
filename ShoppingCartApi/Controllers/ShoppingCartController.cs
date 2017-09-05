using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoppingCartApi.Model;
using ShoppingCartApi.Services;
using ShoppingCartApi.Infastructure;

namespace ShoppingCartApi.Controllers
{
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartRepository _repository;
        private readonly IPaymentProvider _paymentProvider;
        private readonly INotificationProvider _notificationProvider;
        private readonly ShoppingCartContext _shoppingCartContext;

        public ShoppingCartController(IShoppingCartRepository repository, IPaymentProvider paymentProvider, INotificationProvider notificationProvider, ShoppingCartContext shoppingCartContext)
        {
            _repository = repository;
            _paymentProvider = paymentProvider;
            _notificationProvider = notificationProvider;
            _shoppingCartContext = shoppingCartContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string customer)
        {
            var shoppingCart = await _repository.GetShoppingCartAsync(customer);

            return Ok(shoppingCart);
        }

        // POST /value
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]ShoppingCart value)
        {
            var shoppingCart = await _repository.UpdateShoppingCartAsync(value);

            return Ok(shoppingCart);
        }

        [Route("checkout")]
        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody]Order order)
        {
            var customer = _shoppingCartContext.Customer.SingleOrDefault(c => c.Name == order.Customer);
            if(customer == null)
            {
                return BadRequest();
            }

            order.Id = Guid.NewGuid();

            var shoppingCart = await _repository.GetShoppingCartAsync(order.Customer);
            if (shoppingCart == null)
            {
                return BadRequest();
            }
            order.Date = DateTime.Now;

            if (customer.Type == CustomerType.Silver)
                order.Amount = order.Amount * (decimal)0.98;
            else if (customer.Type == CustomerType.Silver)
                order.Amount = order.Amount * (decimal)0.97;

            var lastYearOrders = _shoppingCartContext.Order.Where(x => x.Date >= DateTime.Now.AddYears(-1)).Sum(x=>x.Amount) + order.Amount;
            var oldCustomerType = customer.Type;
            if(lastYearOrders >= 500  && lastYearOrders < 800)
            {
                customer.Type = CustomerType.Silver;                
            }
            else if(lastYearOrders >= 800)
            {
                customer.Type = CustomerType.Gold;
            }

            if(customer.Type != oldCustomerType)
            {                
                _shoppingCartContext.Update(customer);
            }

            var orderLine = new OrderLine()
            {
                OrderId = order.Id,
                Products = shoppingCart.Products
            };

            _shoppingCartContext.Order.Add(order);
            _shoppingCartContext.OrderLine.Add(orderLine);
            await _shoppingCartContext.SaveChangesAsync();

            if (!_paymentProvider.Authorize(customer.Name, order.Amount))
            {
                return BadRequest();
            }

            //Notify customer
            await _notificationProvider.SendEmailAsync(customer.Email, $"Order {order.Id} is placed", "Thank you for your order");

            //Notify courier
            await _notificationProvider.SendEmailAsync("courier@localhost", $"Order {order.Id} is placed", $"The order will be send to {order.Address}");

            return Ok(order);
        }
    }
}
