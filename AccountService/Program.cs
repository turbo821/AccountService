using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Services;
using AccountService.Middlewares;
using FluentValidation;
using MediatR;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StubDbContext>();

builder.Services.AddMediatR(cfg 
    => cfg.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddAutoMapper(cfg 
    => cfg.AddProfile<MappingProfile>());

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddSingleton<IOwnerVerificator, OwnerVerificatorStub>();
builder.Services.AddSingleton<ICurrencyValidator, CurrencyValidatorStub>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
