
using AccountService.Features.Accounts;
using AccountService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<StubDbContext>();

builder.Services.AddMediatR(cfg 
    => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddAutoMapper(cfg 
    => cfg.AddProfile<MappingProfile>());

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
