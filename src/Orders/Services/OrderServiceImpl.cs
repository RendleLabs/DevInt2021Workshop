using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ingredients.Protos;
using Orders.Protos;
using Orders.PubSub;

namespace Orders.Services;

public class OrderServiceImpl : OrderService.OrderServiceBase
{
    private readonly IngredientsService.IngredientsServiceClient _ingredients;
    private readonly IOrderMessages _orderMessages;
    private readonly IOrderPublisher _orderPublisher;

    public OrderServiceImpl(IngredientsService.IngredientsServiceClient ingredients,
        IOrderMessages orderMessages, IOrderPublisher orderPublisher)
    {
        _ingredients = ingredients;
        _orderMessages = orderMessages;
        _orderPublisher = orderPublisher;
    }

    public override async Task<PlaceOrderResponse> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
    {
        var decrementCrustsRequest = new DecrementCrustsRequest
        {
            CrustId = request.CrustId
        };
        var crustTask = _ingredients.DecrementCrustsAsync(decrementCrustsRequest);

        var decrementToppingsRequest = new DecrementToppingsRequest
        {
            ToppingIds =
            {
                request.ToppingsIds
            }
        };
        var toppingTask = _ingredients.DecrementToppingsAsync(decrementToppingsRequest);

        await Task.WhenAll(crustTask.ResponseAsync, toppingTask.ResponseAsync);

        var expectedTime = DateTimeOffset.UtcNow.AddHours(0.5d);

        await _orderPublisher.PublishOrder(request.CrustId, request.ToppingsIds, expectedTime);

        return new PlaceOrderResponse
        {
            ExpectedTime = expectedTime.ToTimestamp()
        };
    }

    public override async Task Subscribe(SubscribeRequest request, IServerStreamWriter<OrderNotification> responseStream, ServerCallContext context)
    {
        var token = context.CancellationToken;

        while (!token.IsCancellationRequested)
        {
            try
            {
                var message = await _orderMessages.ReadAsync(token);
                var notification = new OrderNotification
                {
                    CrustId = message.CrustId,
                    ToppingIds = { message.ToppingIds },
                    ExpectedTime = message.Time.ToTimestamp()
                };
                try
                {
                    await responseStream.WriteAsync(notification);
                }
                catch
                {
                    await _orderPublisher.PublishOrder(message.CrustId, message.ToppingIds, message.Time);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}