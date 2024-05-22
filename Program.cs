using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Npgsql;
using SocialnetworkHomework.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Reflection;

namespace SocialnetworkHomework
{
    public class Program
    {

        private static void Main(string[] args)
        {
            using NpgsqlConnection conn = new("Server=host.docker.internal;Port=5432; Database=baeldung;User Id=baeldung;Password=baeldung;");

            Actions action = new Actions(conn);

            string version = "v1";

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

            WebApplication app = builder.Build();
            app.UseRouting();
#if DEBUG
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = string.Empty;
                options.SwaggerEndpoint($"/swagger/{version}/swagger.json", version);
            });
#endif

            app.UseHttpsRedirection();

            app.MapPost($"{version}" + "/user", (RegistrationData regData) =>
            {
                return action.UserCreate(regData);
            })
            .WithName("UserCreate")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapGet($"{version}" + "/user", (Guid userId) =>
            {
                return action.UserGet(userId);
            })
            .WithName("UserGet")
            .Produces(StatusCodes.Status200OK, typeof(UserInfo))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapDelete($"{version}" + "/user", (Guid userId, AuthRequestData authData) =>
            {
                return action.UserDelete(userId, authData);
            })
            .WithName("UserDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPut($"{version}" + "/user", (Guid userId, UserCommonData userInfo) =>
            {
                return action.UserUpdate(userId, userInfo);
            })
            .WithName("UserUpdate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/login", (AuthRequestData authData) =>
            {
                return action.UserLogin(authData);
            })
            .WithName("UserLogin")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/logout", (AuthResponseData authData) =>
            {
                return action.UserLogout(authData);
            })
            .WithName("UserLogout")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.Run();
        }
    }
}
