using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.Api.Middleware;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappers;
using OrderService.Application.Services;
using OrderService.Application.Validators;
using OrderService.Infrastructure.Context;
using OrderService.Infrastructure.Http;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──
builder.Services.AddControllers();

// ── Swagger with JWT Support ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrderService API",
        Version = "v1",
        Description = "eCommerce OrderService with Service Bus and Polly Circuit Breaker"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

// ── Database ──
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("OrderDb"),
        sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

// ── JWT Authentication ──
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── AutoMapper ──
builder.Services.AddAutoMapper(typeof(MapperProfile));

// ── FluentValidation ──
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();

// ── Dependency Injection ──
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();

////// ── Service Bus Publisher with Circuit Breaker ──
builder.Services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
//builder.Services.AddSingleton<IMessagePublisher, DummyMessagePublisher>();

////// ── Service Bus Consumer as Background Service ──
builder.Services.AddHostedService<ServiceBusConsumer>();

// ── ProductService HTTP Client with Polly ──
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>();

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ── Global Exception Middleware — first ──
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto migrate ──
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.Run();
