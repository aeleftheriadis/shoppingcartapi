using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartApi.Model
{
    public class RedisShoppingCartRepository : IShoppingCartRepository
    {
        private ILogger<RedisShoppingCartRepository> _logger;
        private ShoppingCartSettings _settings;

        private ConnectionMultiplexer _redis;

        public RedisShoppingCartRepository(IOptionsSnapshot<ShoppingCartSettings> options, ILoggerFactory loggerFactory)
        {
            _settings = options.Value;
            _logger = loggerFactory.CreateLogger<RedisShoppingCartRepository>();
        }

        public async Task<ShoppingCart> GetShoppingCartAsync(string customer)
        {
            var database = await GetDatabase();

            var data = await database.StringGetAsync(customer);
            if (data.IsNullOrEmpty)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<ShoppingCart>(data);
        }

        public async Task<ShoppingCart> UpdateShoppingCartAsync(ShoppingCart shoppingCart)
        {
            var database = await GetDatabase();

            var created = await database.StringSetAsync(shoppingCart.Customer, JsonConvert.SerializeObject(shoppingCart));
            if (!created)
            {
                _logger.LogInformation("Problem occur persisting the item.");
                return null;
            }

            _logger.LogInformation("ShoppingCart item persisted succesfully.");

            return await GetShoppingCartAsync(shoppingCart.Customer);
        }

        public async Task<bool> DeleteShoppingCartAsync(string customer)
        {
            var database = await GetDatabase();
            return await database.KeyDeleteAsync(customer);
        }

        private async Task<IDatabase> GetDatabase()
        {
            if (_redis == null)
            {
                await ConnectToRedisAsync();
            }

            return _redis.GetDatabase();
        }

        private async Task<IServer> GetServer()
        {
            if (_redis == null)
            {
                await ConnectToRedisAsync();
            }
            var endpoint = _redis.GetEndPoints();

            return _redis.GetServer(endpoint.First());
        }

        private async Task ConnectToRedisAsync()
        {
            var configuration = ConfigurationOptions.Parse(_settings.ConnectionString, true);
            configuration.ResolveDns = true;

            _logger.LogInformation($"Connecting to database {configuration.SslHost}.");
            _redis = await ConnectionMultiplexer.ConnectAsync(configuration);
        }
    }
}
