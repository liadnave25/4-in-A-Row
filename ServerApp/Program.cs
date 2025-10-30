using ServerApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & RazorPages
builder.Services.AddControllers();
builder.Services.AddRazorPages();

// 2. Configure your DbContext
builder.Services.AddDbContext<ConnectFourContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Register Cookie Authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";            // Redirect here if not authenticated
        options.AccessDeniedPath = "/Login";     // Redirect here on 403
    });

// 4. Register Authorization (required before UseAuthorization())
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   
app.UseAuthorization();


app.MapControllers();
app.MapRazorPages();

app.Run();
