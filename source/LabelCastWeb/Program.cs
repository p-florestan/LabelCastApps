using LabelCast;

namespace LabelCastWeb
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews(
                options => options.InputFormatters.Add(new TextPlainInputFormatter())
            );

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

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Initializing custom static configuration store:

            // We always start in Debug log level, and once full configuration is read,
            // it is set to what is contained in "Client.json" config file
            Logger.CurrentLogLevel = Level.Debug;

            // Start web app

            app.Run();
        }
    }
}
