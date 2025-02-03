using YourApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Register the chatbot service as a singleton so it's available throughout the app
builder.Services.AddSingleton<ChatbotService>();

// Add services to the container
builder.Services.AddControllersWithViews();  // Adding MVC support

// Register HttpClient for making HTTP requests (you'll need this for making API calls)
builder.Services.AddHttpClient<ChatbotService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Map controller routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Start the application
app.Run();
