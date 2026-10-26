using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace StudySpace.Worker;

public sealed class Worker(IConfiguration configuration, ILogger<Worker> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration["RabbitMq:Host"] ?? "localhost",
                    UserName = configuration["RabbitMq:User"] ?? "guest",
                    Password = configuration["RabbitMq:Password"] ?? "guest"
                };
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync("studyspace.activity", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await _channel.BasicQosAsync(0, 1, false, stoppingToken);
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += ProcessAsync;
                await _channel.BasicConsumeAsync("studyspace.activity", autoAck: false, consumer, stoppingToken);
                logger.LogInformation("StudySpace worker is consuming studyspace.activity.");
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "RabbitMQ connection failed; retrying in five seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(object sender, BasicDeliverEventArgs args)
    {
        try
        {
            using var payload = JsonDocument.Parse(args.Body.ToArray());
            logger.LogInformation("Processed {Type} for document {DocumentId} at {ProcessedAt}",
                payload.RootElement.GetProperty("Type").GetString(),
                payload.RootElement.GetProperty("DocumentId").GetGuid(),
                DateTimeOffset.UtcNow);
            await Task.Delay(250);
            if (_channel is not null) await _channel.BasicAckAsync(args.DeliveryTag, false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Message processing failed.");
            if (_channel is not null) await _channel.BasicNackAsync(args.DeliveryTag, false, requeue: false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
