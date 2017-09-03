using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Infastructure.Exceptions
{
    public class ShoppingCartDomainException : Exception
    {
        public ShoppingCartDomainException()
        { }

        public ShoppingCartDomainException(string message)
            : base(message)
        { }

        public ShoppingCartDomainException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
