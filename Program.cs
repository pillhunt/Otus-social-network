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
using System.Threading.Tasks;

namespace SocialnetworkHomework
{
    public class Program
    {
        static private SemaphoreSlim requestTaskQueueSemaphore { get; set; } = new SemaphoreSlim(0, 100);
        static private ConcurrentQueue<Task<IResult>> requestTaskQueue { get; set; } = new ConcurrentQueue<Task<IResult>>();

        private static void Main(string[] args)
        {
            RequestActions requestActions = new RequestActions();

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

            builder.Services.AddHostedService<RequestManager>(serviceProvider => new RequestManager(requestTaskQueueSemaphore, requestTaskQueue));

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

            app.MapPost($"{version}" + "/user", (RegistrationData regData) => requestActions.UserCreate(regData))
            .WithName("UserCreate")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapGet($"{version}" + "/user", async (Guid userId) => 
            { 
                return await UserGetAsync(requestActions, userId); 
            })
            .WithName("UserGet")
            .Produces(StatusCodes.Status200OK, typeof(UserInfo))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapDelete($"{version}" + "/user", (Guid userId) => requestActions.UserDelete(userId))
            .WithName("UserDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPut($"{version}" + "/user", (Guid userId, UserEditData userInfo) => requestActions.UserUpdate(userId, userInfo))
            .WithName("UserUpdate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/login", (AuthRequestData authData) => requestActions.UserLogin(authData))
            .WithName("UserLogin")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/logout", (AuthResponseData authData) => requestActions.UserLogout(authData))
            .WithName("UserLogout")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPost($"{version}" + "/user/search", async (UserBaseData userData) => 
            {
                return await UserSearchAsync(requestActions, userData);
            })
            .WithName("UserSearch")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.Run();
        }

        private static async Task<IResult> UserSearchAsync(RequestActions requestActions, UserBaseData userData)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> userSearcCallTask = Task.Run(async () => await requestActions.UserSearch(userData, taskSemaphore)); 
            requestTaskQueue.Enqueue(userSearcCallTask);

            requestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await userSearcCallTask;
        }

        private static async Task<IResult> UserGetAsync(RequestActions requestActions, Guid userId)
        {
            SemaphoreSlim taskSemaphore = new SemaphoreSlim(0);
            Task<IResult> userSearcCallTask = Task.Run(async () => await requestActions.UserGet(userId, taskSemaphore));
            requestTaskQueue.Enqueue(userSearcCallTask);

            requestTaskQueueSemaphore.Release();

            await taskSemaphore.WaitAsync();

            return await userSearcCallTask;
        }
    }
}
