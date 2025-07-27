using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Services;
using AccountService.Middlewares;
using FluentValidation;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StubDbContext>();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddAutoMapper(cfg 
    => cfg.AddProfile<MappingProfile>());

builder.Services.AddScoped<IOwnerVerificator, OwnerVerificatorStub>();
builder.Services.AddScoped<ICurrencyValidator, CurrencyValidatorStub>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();
