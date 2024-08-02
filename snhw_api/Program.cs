using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.OpenApi.Models;

using SocialnetworkHomework.Data;
using SocialnetworkHomework.Workers;

namespace SocialnetworkHomework
{
    public class Program
    {
        private const string version = "v1";
        
        private static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            string db_master_cs = builder.Configuration.GetConnectionString("db_master") 
                ?? throw new Exception("Не удалось получить строку подключения к БД.");

            string redis_cs = builder.Configuration.GetConnectionString("redis")
                ?? throw new Exception("Не удалось получить строку подключения к Redis.");

            RequestActions requestActions = new RequestActions(db_master_cs);

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
            builder.Services.AddHostedService(service => new PostProcessor(requestActions));

            // добавляется Redis
            builder.Services.AddStackExchangeRedisCache(options => 
                options.Configuration = redis_cs);

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

            SetEndPoints(ref app, requestActions);

            app.Run();
        }

        private static void SetEndPoints(ref WebApplication app, RequestActions requestActions)
        {
            #region User section

            app.MapPost($"{version}" + "/user", (RegistrationData regData) => requestActions.UserCreate(regData))
            .WithName("UserCreate")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapGet($"{version}" + "/user", async (Guid userId) => await requestActions.UserGetAsync(userId))
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

            app.MapPost($"{version}" + "/user/search", async (UserBaseData userData) => await requestActions.UserSearchAsync(userData))
            .WithName("UserSearch")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            #endregion

            #region Friend section

            app.MapPost($"{version}" + "/friend", async (Guid userId, ContactData contactData) => await requestActions.FriendAddAsync(userId, contactData))
            .WithName("FriendAdd")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapDelete($"{version}" + "/friend", async (Guid userId, Guid contactId) => await requestActions.FriendDeleteAsync(userId, contactId))
            .WithName("FriendDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            #endregion

            #region Post section

            app.MapPost($"{version}" + "/post", async (Guid userId, string text) => await requestActions.PostCreateAsync(userId, text))
            .WithName("PostCreate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapGet($"{version}" + "/post", async (Guid userId, Guid postId) => await requestActions.PostGetAsync(userId, postId))
            .WithName("PostGet")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapDelete($"{version}" + "/post", async (Guid userId, Guid postId) => await requestActions.PostDeleteAsync(userId, postId))
            .WithName("PostDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPut($"{version}" + "/post", async (Guid userId, Guid postId, PostEditData editData) => await requestActions.PostUpdateAsync(userId, postId, editData))
            .WithName("PostUpdate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            app.MapPut($"{version}" + "/feed", async (Guid userId, IDistributedCache cache) => await requestActions.FeedGetAsync(userId, cache))
            .WithName("FeedGet")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            #endregion
        }
    }
}
