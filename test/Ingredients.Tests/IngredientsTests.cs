using System.Threading.Tasks;
using Ingredients.Protos;
using Xunit;

namespace Ingredients.Tests;

public class IngredientsTests : IClassFixture<IngredientsApplicationFactory>
{
    private readonly IngredientsService.IngredientsServiceClient _client;

    public IngredientsTests(IngredientsApplicationFactory factory)
    {
        _client = factory.GetGrpcClient();
    }
    
    [Fact]
    public async Task GetsToppings()
    {
        var request = new GetToppingsRequest();
        var response = await _client.GetToppingsAsync(request);
        Assert.NotEmpty(response.Toppings);
    }
}