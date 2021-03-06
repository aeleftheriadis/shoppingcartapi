﻿using System;
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
using ShoppingCartApi.Services;
using ShoppingCartApi.Services.Implementations;
using ShoppingCartApi.Services.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ShoppingCartApi.Middlewares;
using ShoppingCartApi.Infastructure.Filters;
using ShoppingCartApi.Infastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;
using System.Data.SqlClient;
using Polly;

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
        public void ConfigureServices(IServiceCollection services)
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

            services.AddDbContext<ShoppingCartContext>(opt => opt.UseInMemoryDatabase());

            //services.AddDbContext<ShoppingCartContext>(options =>
            //{
            //    options.UseSqlServer(Configuration["DBConnectionString"],
            //    sqlServerOptionsAction: sqlOptions =>
            //    {
            //        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
            //        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            //    });

            //    // Changing default behavior when client evaluation occurs to throw. 
            //    // Default in EF Core would be to log a warning when client evaluation is performed.
            //    options.ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.QueryClientEvaluationWarning));
            //    //Check Client vs. Server evaluation: https://docs.microsoft.com/en-us/ef/core/querying/client-eval
            //});

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseCors("CorsPolicy");

            // Add JWT generation endpoint:
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("SecretKey").Value));
            var options = new TokenProviderOptions
            {
                Audience = "ShoppingApiAudience",
                Issuer = "ShoppingApiIssuer",
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
                ValidIssuer = "ShoppingApiIssuer",

                // Validate the JWT Audience (aud) claim
                ValidateAudience = true,
                ValidAudience = "ShoppingApiAudience",

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
            });

            var context = (ShoppingCartContext)app
            .ApplicationServices.GetService(typeof(ShoppingCartContext));

            WaitForSqlAvailabilityAsync(context, loggerFactory, app, env).Wait();

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();            
        }

        private async Task WaitForSqlAvailabilityAsync(ShoppingCartContext ctx, ILoggerFactory loggerFactory, IApplicationBuilder app, IHostingEnvironment env, int retries = 0)
        {
            var logger = loggerFactory.CreateLogger(nameof(Startup));
            var policy = CreatePolicy(retries, logger, nameof(WaitForSqlAvailabilityAsync));
            await policy.ExecuteAsync(async () =>
            {
                await ShoppingCartContextSeed.SeedAsync(app, env, loggerFactory);
            });
        }

        private Policy CreatePolicy(int retries, ILogger logger, string prefix)
        {
            return Policy.Handle<SqlException>().
                WaitAndRetryAsync(
                    retryCount: retries,
                    sleepDurationProvider: retry => TimeSpan.FromSeconds(5),
                    onRetry: (exception, timeSpan, retry, ctx) =>
                    {
                        logger.LogTrace($"[{prefix}] Exception {exception.GetType().Name} with message ${exception.Message} detected on attempt {retry} of {retries}");
                    }
                );
        }
    }
}
