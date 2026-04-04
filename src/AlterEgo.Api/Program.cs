using System.Text;
using AlterEgo.Core.Entities;
using AlterEgo.Core.Enums;
using AlterEgo.Core.Services;
using AlterEgo.Infrastructure.Autofac;
using AlterEgo.Infrastructure.Db;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/alterego-.log", rollingInterval: RollingInterval.Day));

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<InfrastructureModule>();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Alter Ego API",
        Version = "v1",
        Description = "Backend API for Alter Ego application"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

var databaseProvider = builder.Configuration["Database:Provider"];
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider == "PostgreSQL")
    {
        options.UseNpgsql(builder.Configuration["Database:ConnectionString"]);
    }
    else
    {
        options.UseInMemoryDatabase("alterego-db");
    }
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorization();

var app = builder.Build();

await CreateAdminUserAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();

app.MapControllers();

app.Run();

static async Task CreateAdminUserAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (configuration["Database:Provider"] == "PostgreSQL")
    {
        await context.Database.MigrateAsync();
    }

    var adminUsername = configuration["AdminUser:Username"];
    if (string.IsNullOrEmpty(adminUsername))
    {
        return;
    }

    if (await context.Users.AnyAsync(x => x.Username == adminUsername))
    {
        return;
    }

    var adminUser = new UserEntity
    {
        Id = Guid.NewGuid(),
        Username = adminUsername,
        TelegramId = long.Parse(configuration["AdminUser:TelegramId"] ?? "0"),
        PasswordHash = passwordHasher.Hash(configuration["AdminUser:Password"]!),
        Role = Role.Admin,
        CreatedAt = DateTimeOffset.UtcNow
    };

    context.Users.Add(adminUser);
    await context.SaveChangesAsync();

    logger.LogInformation("Admin user '{Username}' created", adminUsername);
}