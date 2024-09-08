using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using snhw.Data;

namespace snhw
{
    public static class ActionGroups
    {
        static Actions actions = new Actions();

        public static RouteGroupBuilder UserGroup(this RouteGroupBuilder group)
        {
            group.MapPost("/user", (RegistrationData regData) => actions.UserCreate(regData))
            .WithName("UserCreate")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapGet("/user", async (Guid userId) => await actions.UserGetAsync(userId))
            .WithName("UserGet")
            .Produces(StatusCodes.Status200OK, typeof(UserInfo))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapDelete("/user", (Guid userId) => actions.UserDelete(userId))
            .WithName("UserDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapPut("/user", (Guid userId, UserEditData userInfo) => actions.UserUpdate(userId, userInfo))
            .WithName("UserUpdate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapPost("/user/login", (AuthRequestData authData) => actions.UserLogin(authData))
            .WithName("UserLogin")
            .Produces(StatusCodes.Status200OK, typeof(AuthResponseData))
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapPost("/user/logout", (AuthResponseData authData) => actions.UserLogout(authData))
            .WithName("UserLogout")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapPost("/user/search", async (UserBaseData userData) => await actions.UserSearchAsync(userData))
            .WithName("UserSearch")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            return group;
        }

        public static RouteGroupBuilder ContactGroup(this RouteGroupBuilder group)
        {
            group.MapGet("/contact", async (Guid userId, [FromBody] Guid contactUserId) => await actions.ContactGetAsync(userId, contactUserId))
            .WithName("ContactGet")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group
            .MapPost("/contact", async (Guid userId, [FromBody] ContactData contactData) => await actions.ContactAddAsync(userId, contactData))
            .WithName("ContactAdd")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapPut("/contact", async (Guid userId, [FromBody] ContactData contactData) => await actions.ContactUpdateAsync(userId, contactData))

            .WithName("ContactUpdate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapDelete("/contact", async (Guid userId, Guid contactId) => await actions.ContactDeleteAsync(userId, contactId))
            .WithName("ContactDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            return group;
        }
        public static RouteGroupBuilder PostGroup(this RouteGroupBuilder group)
        {
            #region Post section

            group.MapPost("/post", async (Guid userId, [FromBody] string text) => await actions.PostCreateAsync(userId, text))
            .WithName("PostCreate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapGet("/post", async (Guid userId, Guid postId) => await actions.PostGetAsync(userId, postId))
            .WithName("PostGet")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapDelete("/post", async (Guid userId, [FromBody] Guid postId) => await actions.PostDeleteAsync(userId, postId))
            .WithName("PostDelete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapPut("/post", async (Guid userId, [FromBody] PostEditData editData) => await actions.PostUpdateAsync(userId, editData))
            .WithName("PostUpdate")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapGet("/feed", async (Guid userId, IDistributedCache cache) => await actions.FeedGetAsync(userId, cache))
            .WithName("FeedGet")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            #endregion

            return group;
        }

        public static RouteGroupBuilder DialogGroup(this RouteGroupBuilder group)
        {
            string commonName = "Dialog";

            group.MapPost("/dialog", async ([FromBody] DialogDataEdit dialogData) => await actions.DialogCreateAsync(dialogData))
            .WithName($"{commonName}Create")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapGet("/dialog", async (Guid userId, Guid contactId) => await actions.DialogGetAsync(userId, contactId))
            .WithName($"{commonName}Get")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapDelete("/dialog", async ([FromBody] DialogData dialogData) => await actions.DialogDeleteAsync(dialogData))
            .WithName($"{commonName}Delete")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            group.MapPut("/dialog", async ([FromBody] DialogDataEdit editData) => await actions.DialogUpdateAsync(editData))
            .WithName($"{commonName}Update")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest, typeof(InfoData))
            .Produces(StatusCodes.Status404NotFound, typeof(InfoData))
            .Produces(StatusCodes.Status500InternalServerError, typeof(InfoData))
            ;

            return group;
        }
    }
}
