using System.Text.Json;
using RabbitMQ.Client;

namespace StudySpace.Web.Services;

public sealed record DocumentActivity(string Type, Guid DocumentId, Guid WorkspaceId, string Title, DateTimeOffset OccurredAt);

public interface IActivityQueue
{
    Task PublishAsync(DocumentActivity activity, CancellationToken cancellationToken);
}

public sealed class RabbitActivityQueue(IConfiguration configuration, ILogger<RabbitActivityQueue> logger) : IActivityQueue
{
    public async Task PublishAsync(DocumentActivity activity, CancellationToken cancellationToken)
    {
        var host = configuration["RabbitMq:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogInformation("Queue event {EventType} kept in local-only mode.", activity.Type);
            return;
        }

        try
        {
            var factory = new ConnectionFactory { HostName = host, UserName = configuration["RabbitMq:User"] ?? "guest", Password = configuration["RabbitMq:Password"] ?? "guest" };
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeclareAsync("studyspace.activity", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            var body = JsonSerializer.SerializeToUtf8Bytes(activity);
            var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };
            await channel.BasicPublishAsync(string.Empty, "studyspace.activity", mandatory: true, properties, body, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "RabbitMQ is unavailable; request processing continues.");
        }
    }
}
