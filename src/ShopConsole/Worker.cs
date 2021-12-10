using Grpc.Core;
using Orders.Protos;

namespace ShopConsole;

public class Worker : BackgroundService
{
    private readonly OrderService.OrderServiceClient _orders;
    private readonly ILogger<Worker> _logger;

    public Worker(OrderService.OrderServiceClient orders, ILogger<Worker> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var call = _orders.Subscribe(new SubscribeRequest(), cancellationToken: stoppingToken);
                await foreach (var order in call.ResponseStream.ReadAllAsync(stoppingToken))
                {
                    Console.WriteLine($"Order received: {order.CrustId}");
                    foreach (var toppingId in order.ToppingIds)
                    {
                        Console.WriteLine($"  {toppingId}");
                    }

                    Console.WriteLine($"Due by: {order.ExpectedTime.ToDateTimeOffset():t}");
                }
            }
            catch (OperationCanceledException)
            {
                if (stoppingToken.IsCancellationRequested) break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error: {Message}", ex.Message);
                _logger.LogInformation("Reconnecting in 1 second...");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
