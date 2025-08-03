using AccountService.Application.Behaviors;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Services;
using AccountService.Middlewares;
using FluentValidation;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using AccountService.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<StubDbContext>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ExceptionBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

builder.Services.AddAutoMapper(cfg 
    => cfg.AddProfile<MappingProfile>());

builder.Services.AddScoped<IOwnerVerificator, OwnerVerificatorStub>();
builder.Services.AddScoped<ICurrencyValidator, CurrencyValidatorStub>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.Audience = builder.Configuration["Keycloak:Audience"];
        o.MetadataAddress = builder.Configuration["Keycloak:MetadataAddress"]!;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Keycloak:ValidIssue"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Keycloak:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerGenWithAuth(builder.Configuration);

var app = builder.Build();

app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.Run();
