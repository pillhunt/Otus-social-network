using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Npgsql;
using SocialnetworkHomework.Data;
using SocialnetworkHomework.workers;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Concurrent;
using System.Reflection;

namespace SocialnetworkHomework
{
    public class Program
    {       

        private static void Main(string[] args)
        {
            RequestActions action = new RequestActions();

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

            builder.Services.AddHostedService<RequestManager>(serviceProvider => new RequestManager());

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

            app.UseHttpsRedirection();
            app.UseHsts();

            app.MapPost($"{version}" + "/user", (RegistrationData regData) => action.UserCreate(regData))
            .WithName("UserCreate")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapGet($"{version}" + "/user", (Guid userId) => action.UserGet(userId))
            .WithName("UserGet")
            .Produces(StatusCodes.Status200OK, typeof(UserInfo))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapDelete($"{version}" + "/user", (Guid userId) => action.UserDelete(userId))
            .WithName("UserDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPut($"{version}" + "/user", (Guid userId, UserEditData userInfo) => action.UserUpdate(userId, userInfo))
            .WithName("UserUpdate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/login", (AuthRequestData authData) => action.UserLogin(authData))
            .WithName("UserLogin")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/logout", (AuthResponseData authData) => action.UserLogout(authData))
            .WithName("UserLogout")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/search", (UserBaseData userData) => action.UserSearch(userData))
            .WithName("UserSearch")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.Run();
        }
    }
}
