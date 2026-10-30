using System.Text;

using ECommerceManagement.API.Data;
using ECommerceManagement.API.Repositories;
using ECommerceManagement.API.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// DATABASE
// ======================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ======================================================
// CONTROLLERS
// ======================================================

builder.Services.AddControllers();

// ======================================================
// REPOSITORIES
// ======================================================

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// ======================================================
// SERVICES
// ======================================================

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

// ======================================================
// JWT AUTHENTICATION
// ======================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT Key is missing from appsettings.json."
    );
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            ValidateIssuer = false,
            ValidateAudience = false,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
    });

// ======================================================
// AUTHORIZATION
// ======================================================

builder.Services.AddAuthorization();

// ======================================================
// CORS - REACT
// ======================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ======================================================
// SWAGGER
// ======================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Description =
                "Enter JWT token. Example: Bearer {token}"
        }
    );

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer",
                document
            )] = new List<string>()
        }
    );
});

// ======================================================
// BUILD APP
// ======================================================

var app = builder.Build();

// ======================================================
// DATABASE DIAGNOSTIC
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    var connection = db.Database.GetDbConnection();

    Console.WriteLine();
    Console.WriteLine("==============================================");
    Console.WriteLine("       E-COMMERCE DATABASE DIAGNOSTIC");
    Console.WriteLine("==============================================");

    Console.WriteLine($"DATABASE : {connection.Database}");
    Console.WriteLine($"SERVER   : {connection.DataSource}");

    try
    {
        await connection.OpenAsync();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                DB_NAME() AS DatabaseName,
                OBJECT_SCHEMA_NAME(
                    OBJECT_ID('dbo.Products')
                ) AS SchemaName,
                OBJECT_NAME(
                    OBJECT_ID('dbo.Products')
                ) AS TableName,
                COL_LENGTH(
                    'dbo.Products',
                    'IsActive'
                ) AS IsActiveLength;
        ";

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var databaseName =
                reader["DatabaseName"]?.ToString();

            var schemaName =
                reader["SchemaName"]?.ToString();

            var tableName =
                reader["TableName"]?.ToString();

            var isActive =
                reader["IsActiveLength"]?.ToString();

            Console.WriteLine("----------------------------------------------");
            Console.WriteLine($"DB NAME  : {databaseName}");
            Console.WriteLine($"SCHEMA   : {schemaName}");
            Console.WriteLine($"TABLE    : {tableName}");
            Console.WriteLine($"IsActive : {isActive}");
            Console.WriteLine("----------------------------------------------");

            if (string.IsNullOrEmpty(isActive))
            {
                Console.WriteLine(
                    "WARNING: IsActive COLUMN DOES NOT EXIST!"
                );
            }
            else
            {
                Console.WriteLine(
                    "SUCCESS: IsActive COLUMN EXISTS!"
                );
            }
        }

        await reader.CloseAsync();
        await connection.CloseAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine("----------------------------------------------");
        Console.WriteLine("DATABASE DIAGNOSTIC ERROR");
        Console.WriteLine(ex.Message);
        Console.WriteLine("----------------------------------------------");

        if (connection.State == System.Data.ConnectionState.Open)
        {
            await connection.CloseAsync();
        }
    }

    Console.WriteLine("==============================================");
    Console.WriteLine();
}

// ======================================================
// SWAGGER
// ======================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ======================================================
// HTTP PIPELINE
// ======================================================

app.UseHttpsRedirection();

app.UseCors("AllowReact");

app.UseAuthentication();

app.UseAuthorization();

// ======================================================
// CONTROLLERS
// ======================================================

app.MapControllers();

// ======================================================
// RUN
// ======================================================

app.Run();
