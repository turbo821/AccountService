using AccountService.Application.Abstractions;
using AccountService.Application.Behaviors;
using AccountService.Background;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.Abstractions;
using AccountService.Infrastructure.Consumers;
using AccountService.Infrastructure.Persistence.Repositories;
using AccountService.Infrastructure.Services;
using FluentMigrator.Runner;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using RabbitMQ.Client;
using System.Data;
using System.Reflection;
using AccountService.Application;
using IConnectionFactory = RabbitMQ.Client.IConnectionFactory;

namespace AccountService.Extensions;

public  static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? env = null)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        if (env != null && env.IsEnvironment("IntegrationTests"))
            return services;

        services.AddScoped<IDbConnection>(_
            => new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection")));

        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(configuration.GetConnectionString("DefaultConnection"))
                .ScanIn(Assembly.GetAssembly(typeof(Account))).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        return services;
    }

    public static IServiceCollection AddServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountDapperRepository>();
        services.AddScoped<IOutboxRepository, OutboxDapperRepository>();
        services.AddScoped<IInboxRepository, InboxDapperRepository>();

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
        return services;
    }

    public static IServiceCollection AddHangfireWithPostgres(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? env = null)
    {
        if (env != null && env.IsEnvironment("IntegrationTests"))
            return services;

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

        return services;
    }

    public static IServiceCollection AddSwaggerGenWithAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "AccountService API", Version = "v1" });

            //options.SwaggerDoc("events", new OpenApiInfo
            //{
            //    Title = "AccountService Events",
            //    Version = "v1",
            //    Description = "Контракты доменных событий"
            //});

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            // options.DocumentFilter<RabbitMQEventsDocumentFilter>();
            //var eventTypes = Assembly.GetExecutingAssembly()
            //    .GetTypes()
            //    .Where(t => t.IsSubclassOf(typeof(DomainEvent)))
            //    .ToArray();

            //foreach (var type in eventTypes)
            //{
            //    options.MapType(type, () => new OpenApiSchema
            //    {
            //        Type = "object",
            //        Description = $"Событие {type.Name}"
            //    });
            //}


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

    public static IServiceCollection AddRabbitMq(this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? env = null)
    {
        if (env != null && env.IsEnvironment("IntegrationTests"))
            return services;

        services.AddSingleton<IConnectionFactory>(_ =>
            new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"]!,
                Port = configuration["RabbitMQ:Port"] != null
                    ? int.Parse(configuration["RabbitMQ:Port"]!)
                    : AmqpTcpEndpoint.UseDefaultPort,
                UserName = configuration["RabbitMQ:Username"]!,
                Password = configuration["RabbitMQ:Password"]!
            });

        services.AddSingleton<IBrokerService, RabbitMqService>();

        services.AddScoped<IConsumerHandler, AntifraudConsumer>();
        services.AddScoped<IConsumerHandler, AuditConsumer>();

        services.AddScoped<IRabbitMqHealthChecker, RabbitMqHealthChecker>();

        services.AddHostedService<ConsumersHostedService>();

        return services;
    }
}