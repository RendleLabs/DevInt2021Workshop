using Grpc.Core;
using Ingredients.Data;
using Ingredients.Protos;

namespace Ingredients.Services;

public class IngredientsServiceImpl : IngredientsService.IngredientsServiceBase
{
    private readonly IToppingData _toppingData;
    private readonly ILogger<IngredientsServiceImpl> _logger;

    public IngredientsServiceImpl(IToppingData toppingData, ILogger<IngredientsServiceImpl> logger)
    {
        _toppingData = toppingData;
        _logger = logger;
    }

    public override async Task<GetToppingsResponse> GetToppings(GetToppingsRequest request, ServerCallContext context)
    {
        try
        {
            var toppings = await _toppingData.GetAsync(context.CancellationToken);
            var response = new GetToppingsResponse
            {
                Toppings =
                {
                    toppings.Select(t => new Protos.Topping
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Price = t.Price
                    })
                }
            };
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetToppings cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}