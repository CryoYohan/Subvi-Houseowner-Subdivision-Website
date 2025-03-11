using ELNET1_GROUP_PROJECT.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ELNET1_GROUP_PROJECT.Models; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<MyAppDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllersWithViews();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/home");
        return;
    }
    await next();
});

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        ctx.Context.Response.Headers.Append("Pragma", "no-cache");
        ctx.Context.Response.Headers.Append("Expires", "0");
    }
});

// Enable session middleware
app.UseSession();

app.UseMiddleware<RoleMiddleware>();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Landing}/{id?}");

    endpoints.MapControllerRoute(
        name: "about",
        pattern: "about-us",
        defaults: new { controller = "Home", action = "AboutUs" });

    endpoints.MapControllerRoute(
        name: "contacts",
        pattern: "contacts",
        defaults: new { controller = "Home", action = "Contacts" });

    endpoints.MapControllerRoute(
        name: "admin",
        pattern: "admin/dashboard",
        defaults: new { controller = "Admin", action = "Dashboard" });

    endpoints.MapControllerRoute(
        name: "staff",
        pattern: "staff/dashboard",
        defaults: new { controller = "Staff", action = "Staffdashboard" });

    endpoints.MapControllerRoute(
        name: "homeowner",
        pattern: "home/dashboard",
        defaults: new { controller = "Home", action = "dashboard" });
});



app.Run();
