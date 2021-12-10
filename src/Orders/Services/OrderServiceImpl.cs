using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ingredients.Protos;
using Orders.Protos;

namespace Orders.Services;

public class OrderServiceImpl : OrderService.OrderServiceBase
{
    private readonly IngredientsService.IngredientsServiceClient _ingredients;

    public OrderServiceImpl(IngredientsService.IngredientsServiceClient ingredients)
    {
        _ingredients = ingredients;
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

        return new PlaceOrderResponse
        {
            ExpectedTime = expectedTime.ToTimestamp()
        };
    }
}