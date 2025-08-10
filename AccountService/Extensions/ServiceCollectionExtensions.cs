using AccountService.Application.Abstractions;
using AccountService.Application.Behaviors;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Services;
using FluentMigrator.Runner;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Data;
using System.Reflection;
using Hangfire;
using Hangfire.PostgreSql;
using Npgsql;

namespace AccountService.Extensions;

public  static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddScoped<IDbConnection>(_
            => new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection")));

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("DefaultConnection"))
                .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceCollection AddServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountDapperRepository>();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Program>();
            cfg.AddOpenBehavior(typeof(ExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

        services.AddAutoMapper(cfg
            => cfg.AddProfile<MappingProfile>());

        services.AddScoped<IOwnerVerificator, OwnerVerificatorStub>();
        services.AddScoped<ICurrencyValidator, CurrencyValidator>();

        services.AddHttpClient();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IInterestAccrualService, InterestAccrualService>();

        return services;
    }

    public static IServiceCollection AddHangfireWithPostgres(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire(config =>
            config.UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(opt =>
                    opt.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))
                    ));

        services.AddHangfireServer();
        return services;
    }
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthorization();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.MetadataAddress = configuration["Keycloak:MetadataAddress"]!;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Keycloak:ValidIssue"],
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // services.AddHangfire()
        return services;
    }

    public static IServiceCollection AddSwaggerGenWithAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            options.AddSecurityDefinition("Keycloak OAuth2.0", new OpenApiSecurityScheme
            {
                Description = "1 Способ: Тестовые данные для авторизации: clientId: account-api, " +
                              "username: tom, password: pass123",
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(configuration["Keycloak:AuthorizationUrl"]!),
                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "OpenID" },
                            { "profile", "User Profile" }
                        }
                    }
                }
            });

            options.AddSecurityDefinition("Keycloak JWT", new OpenApiSecurityScheme
            {
                Description = "2 Способ: Введите accessToken, полученный в методе /auth/token",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "Keycloak OAuth2.0",
                            Type = ReferenceType.SecurityScheme
                        },
                        In = ParameterLocation.Header,
                        Name = "Bearer",
                        Scheme = "Bearer"
                    },
                    []
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Keycloak JWT"
                        },
                        In = ParameterLocation.Header,
                        Name = "Authorization"
                    },

                    []
                }
            };
        
            options.AddSecurityRequirement(securityRequirement);
        });

        return services;
    }
}