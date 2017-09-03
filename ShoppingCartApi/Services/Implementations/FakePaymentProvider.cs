using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Services.Implementations
{
    public class FakePaymentProvider : IPaymentProvider
    {
        public bool Authorize(string customer, decimal amount)
        {
            return true;
        }
    }
}
