using _301271988_chauhanpachchigar__Lab3;
using Amazon.Runtime;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddControllersWithViews();

//builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
connectionString.UserID = builder.Configuration["DbUser"];
connectionString.Password = builder.Configuration["DbPassword"];

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString.ConnectionString));

//builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>().AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllersWithViews();

builder.Services.Configure<AWSOptions>(builder.Configuration.GetSection("AWSCredentials"));
builder.Services.AddTransient<Helper>();

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
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();
app.Run();
