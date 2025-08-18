using AccountService.Application.Contracts;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace AccountService.Application;

// TODO: Add XML comments to your domain events and use them in the Swagger documentation
public class RabbitMQEventsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // 1. Добавляем раздел "События" в Swagger
        swaggerDoc.Tags ??= new List<OpenApiTag>();

        if (!swaggerDoc.Tags.Any(t => t.Name == "Events"))
        {
            swaggerDoc.Tags.Add(new OpenApiTag
            {
                Name = "Events",
                Description = "Документация по событиям RabbitMQ",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Подробнее о messaging",
                    Url = new Uri("https://www.rabbitmq.com/documentation.html")
                }
            });
        }

        // 2. Находим все классы, наследующиеся от DomainEvent
        var domainEventTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(DomainEvent)))
            .ToList();

        // 3. Добавляем схемы для всех событий
        foreach (var eventType in domainEventTypes)
        {
            if (!context.SchemaRepository.Schemas.ContainsKey(eventType.Name))
            {
                var schema = context.SchemaGenerator.GenerateSchema(eventType, context.SchemaRepository);

                var eventAttribute = eventType.GetCustomAttribute<RabbitMqEventAttribute>();
                if (eventAttribute != null)
                {
                    schema.Extensions.Add("x-rabbitmq-exchange", new OpenApiString(eventAttribute.Exchange));
                    schema.Extensions.Add("x-rabbitmq-routingKey", new OpenApiString(eventAttribute.RoutingKey));
                }

                schema.Description = GetEventDescription(eventType);
            }
        }

        // 4. Создаем отдельный путь для отображения списка событий
        var eventsPath = new OpenApiPathItem
        {
            Operations = new Dictionary<OperationType, OpenApiOperation>
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    Tags = new List<OpenApiTag> // Изменено на List<OpenApiTag>
                    {
                        new OpenApiTag { Name = "Events" }
                    },
                    Summary = "Получить список всех событий RabbitMQ",
                    Description = "Возвращает метаинформацию о всех событиях системы",
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Список событий",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        AdditionalProperties = new OpenApiSchema
                                        {
                                            Reference = new OpenApiReference
                                            {
                                                Type = ReferenceType.Schema,
                                                Id = "DomainEvent"
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        swaggerDoc.Paths.Add("/api/events", eventsPath);
    }

    private string GetEventDescription(Type eventType)
    {
        var summary = eventType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description
                   ?? "Доменное событие";

        var remarks = eventType.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.GetDescription();

        return string.IsNullOrEmpty(remarks)
            ? summary
            : $"{summary}\n\n{remarks}";
    }
}