using Microsoft.EntityFrameworkCore;
using MediConnectAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MediConnectAPI.Models;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// DB Context: prefer DevConnection in Development, fallback to DefaultConnection
var connectionString = builder.Environment.IsDevelopment()
    ? (builder.Configuration.GetConnectionString("DevConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection"))
    : builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MediConnectContext>(options =>
    options.UseSqlServer(connectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure()));


Console.WriteLine("Connection String: " + connectionString);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Config
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = "sub"
    };
});

builder.Services.AddAuthorization();


// (Opcional) CORS para probar desde frontend local
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MediConnectContext>();
    try
    {
        await db.Database.MigrateAsync();

        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "Doctor" },
                new Role { Name = "Patient" }
            );
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync(u => u.Email == "admin@mediconnect.com"))
        {
            var adminRoleId = await db.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstAsync();
            db.Users.Add(new User
            {
                FirstName = "Admin",
                LastName  = "MediConnect",
                Email     = "admin@mediconnect.com",
                Password  = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                RoleId    = adminRoleId
            });
            await db.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: DB migration/seeding failed: {ex.Message}");
    }
}

app.Run();
