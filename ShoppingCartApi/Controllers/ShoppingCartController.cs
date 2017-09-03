using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShoppingCartApi.Model;
using ShoppingCartApi.Services;

namespace ShoppingCartApi.Controllers
{
    [Route("api/v1/[controller]")]
    //[Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartRepository _repository;
        private readonly IPaymentProvider _paymentProvider;
        private readonly INotificationProvider _notificationProvider;
        //private readonly IIdentityService _identitySvc;
        //private readonly IEventBus _eventBus;
        public ShoppingCartController(IShoppingCartRepository repository, IPaymentProvider paymentProvider, INotificationProvider notificationProvider)
    //IIdentityService identityService,
    //IEventBus eventBus)
        {
            _repository = repository;
            _paymentProvider = paymentProvider;
            _notificationProvider = notificationProvider;
            //_identitySvc = identityService;
            //_eventBus = eventBus;
        }
        // GET /id
        [HttpGet("{id}")]
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
        public async Task<IActionResult> Checkout([FromBody]Order order, [FromBody]Customer customer)
        {
            //var userId = _identitySvc.GetUserIdentity();
            order.Id = Guid.NewGuid();

            var shoppingCart = await _repository.GetShoppingCartAsync(order.Customer);
            if (shoppingCart == null)
            {
                return BadRequest();
            }
            order.Date = DateTime.Now;

            if (customer.Type == CustomerType.Silver)
                order.Amount = order.Amount * (decimal)0.98;
            else if(customer.Type == CustomerType.Silver)
                order.Amount = order.Amount * (decimal)0.97;

            //In order to update the customer I have to retrieve all orders for the past 12 months included the one above
            //from other part of this application since I don't persist the order in this part of the app

            var orderLine = new OrderLine()
            {
                OrderId = order.Id,
                Products = shoppingCart.Products
            };

            if(!_paymentProvider.Authorize(customer.Name, order.Amount))
            {
                return BadRequest();
            }

            //Notify customer
            await _notificationProvider.SendEmailAsync(customer.Email, $"Order {order.Id} is placed","Thank you for your order");

            //Notify courier
            await _notificationProvider.SendEmailAsync("courier@localhost", $"Order {order.Id} is placed", $"The order will be send to {order.Address}");

            return Ok(order);
        }
    }
}
