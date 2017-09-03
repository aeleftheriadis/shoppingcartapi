using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Services
{
    public interface IPaymentProvider
    {
        bool Authorize(string customer, decimal amount);
    }
}
