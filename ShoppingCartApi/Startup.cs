using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.AspNetCore.Http;
using ShoppingCartApi.Model;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using ShoppingCartApi.Services;
using ShoppingCartApi.Services.Implementations;
using ShoppingCartApi.Services.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ShoppingCartApi.Middlewares;
using ShoppingCartApi.Infastructure.Filters;

namespace ShoppingCartApi
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            

            services.Configure<ShoppingCartSettings>(Configuration);
    
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<ShoppingCartSettings>>().Value;
                ConfigurationOptions configuration = ConfigurationOptions.Parse(settings.ConnectionString, true);
                configuration.ResolveDns = true;

                return ConnectionMultiplexer.Connect(configuration);
            });

            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new Info
                {   
                    Title = "ShoppingCart HTTP API",
                    Version = "v1",
                    Description = "The ShoppingCart Service HTTP API",
                    TermsOfService = "Terms Of Service"
                });
                options.OperationFilter<AuthorizationHeaderParameterOperationFilter>();
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IShoppingCartRepository, RedisShoppingCartRepository>();
            services.AddTransient<IPaymentProvider, FakePaymentProvider>();
            services.AddTransient<INotificationProvider, EmailNotificationProvider>();

            services.AddOptions();
            services.AddMvc();
            var container = new ContainerBuilder();
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseCors("CorsPolicy");
            app.UseMvcWithDefaultRoute();
            app.UseStaticFiles();

            // Add JWT generation endpoint:
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("SecretKey").Value));
            var options = new TokenProviderOptions
            {
                Audience = "localhost",
                Issuer = "localhost",
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            };

            app.UseMiddleware<TokenProviderMiddleware>(Options.Create(options));

            var tokenValidationParameters = new TokenValidationParameters
            {
                // The signing key must match!
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                // Validate the JWT Issuer (iss) claim
                ValidateIssuer = true,
                ValidIssuer = "localhost",

                // Validate the JWT Audience (aud) claim
                ValidateAudience = true,
                ValidAudience = "localhost",

                // Validate the token expiry
                ValidateLifetime = true,

                // If you want to allow a certain amount of clock drift, set that here:
                ClockSkew = TimeSpan.Zero
            };

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = tokenValidationParameters
            });

            app.UseSwagger().UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My ShoppingCartApi V1");
                c.InjectOnCompleteJavaScript("https://cdnjs.cloudflare.com/ajax/libs/crypto-js/3.1.9-1/crypto-js.min.js"); // https://cdnjs.com/libraries/crypto-js
                c.InjectOnCompleteJavaScript("/swagger-ui/authorization2.js");
                //c.ConfigureOAuth2("shoppingcartswaggerui", "", "", "Shopping Cart Swagger UI");
            });
        }
    }
}
