namespace AccountService.Application;

[AttributeUsage(AttributeTargets.Class)]
public class RabbitMqEventAttribute(string exchange, string routingKey) : Attribute
{
    public string Exchange { get; } = exchange;
    public string RoutingKey { get; } = routingKey;
}