using AccountService.Extensions;
using AccountService.Middlewares;
using System.Text.Json.Serialization;

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

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddHangfireWithPostgres(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddSwaggerGenWithAuth(builder.Configuration);
builder.Services.AddRabbitMq(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();

app.RunMigrations();
await app.InitializeRabbitMqAsync();
app.UseHangfire();

app.UseCors("AllowAll");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(builder.Configuration["Keycloak:ClientId"]);
        options.OAuthScopes("openid", "profile", "email", "roles");
    });
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;