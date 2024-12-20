using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using snhw_api.Rabbit;
using snhw_api.Workers;
using System.Net.WebSockets;
using System.Net;
using System.Text;

namespace snhw_api
{
    public class Program
    {
        private const string version = "v1";

        private static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            string redis_cs = builder.Configuration.GetConnectionString("redis")
                ?? throw new Exception("Не удалось получить строку подключения к Redis.");

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers().AddNewtonsoftJson();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(version, new OpenApiInfo
                {
                    Version = version,
                    Title = "API Социальной сети",
                    Description = "Домашняя работа Otus"
                });
                c.DescribeAllParametersInCamelCase();
            });

            builder.Services.AddHostedService<RequestManager>();
            builder.Services.AddHostedService<PostingManager>();
            builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();

            // добавляется Redis
            builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redis_cs);

            WebApplication app = builder.Build();
            app.UseRouting();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = string.Empty;
                options.SwaggerEndpoint($"/swagger/{version}/swagger.json", version);
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseWebSockets();
            app.UseHttpsRedirection();
            app.UseHsts();

            app.MapGroup(version).UserGroup().WithTags("User");
            app.MapGroup(version).ContactGroup().WithTags("Contact");
            app.MapGroup(version).DialogGroup().WithTags("Dialog");
            app.MapGroup(version).PostGroup().WithTags("Post");
            app.MapGroup(version).FeedGroup().WithTags("Feed");

            app.Run();
        }        
    }    
}
