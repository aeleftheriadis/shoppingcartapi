using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Services
{
    public interface INotificationProvider
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
