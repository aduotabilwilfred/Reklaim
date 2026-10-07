using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Reklaim.api.Data;
using Reklaim.api.Models;
using Reklaim.api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddControllers();

// Register the file storage service: use Cloudinary if credentials/URL are configured, otherwise fall back to LocalDiskFileStorageService
var hasCloudinary = !string.IsNullOrWhiteSpace(builder.Configuration["CLOUDINARY_URL"])
    || !string.IsNullOrWhiteSpace(builder.Configuration["Cloudinary:Url"])
    || (!string.IsNullOrWhiteSpace(builder.Configuration["Cloudinary:CloudName"] ?? builder.Configuration["CLOUDINARY_CLOUD_NAME"])
        && !string.IsNullOrWhiteSpace(builder.Configuration["Cloudinary:ApiKey"] ?? builder.Configuration["CLOUDINARY_API_KEY"])
        && !string.IsNullOrWhiteSpace(builder.Configuration["Cloudinary:ApiSecret"] ?? builder.Configuration["CLOUDINARY_API_SECRET"]));

if (hasCloudinary)
{
    builder.Services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalDiskFileStorageService>();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "reklaim-api"
}));

// If local disk storage is used, configure static file serving for /uploads
if (!hasCloudinary)
{
    var uploadsFolder = LocalDiskFileStorageService.GetUploadsFolder(app.Environment);
    Directory.CreateDirectory(uploadsFolder);
    var uploadsProvider = new PhysicalFileProvider(uploadsFolder);
    app.Lifetime.ApplicationStopped.Register(uploadsProvider.Dispose);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = uploadsProvider,
        RequestPath = "/uploads"
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
