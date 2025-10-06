using FitHub.Web;              // for StorageFactory
using FitHub.Web.Services;     // for FunctionsClient
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews();

// Your storage factory (reads ConnectionStrings:AzureStorage)
builder.Services.AddSingleton<StorageFactory>();

// ✅ Add a typed HttpClient to call your Azure Functions
builder.Services.AddHttpClient<FunctionsClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Optional: quick route alias for your test page
app.MapControllerRoute(
    name: "part2",
    pattern: "part2/{action=Index}/{id?}",
    defaults: new { controller = "Part2" });

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
