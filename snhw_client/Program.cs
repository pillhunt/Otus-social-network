using snhw_client.Worker;

namespace snhw_client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            Guid.TryParse(args[0].ToString(), out Guid consumerId);
            Console.WriteLine(consumerId);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddHostedService<RabbitMqListener>(listener => new RabbitMqListener(consumerId.ToString()));
            builder.Services.AddHostedService<WebSocketListener>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseWebSockets();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
