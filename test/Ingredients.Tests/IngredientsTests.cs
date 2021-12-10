using System;
using System.Threading.Tasks;
using Ingredients.Protos;
using Xunit;

namespace Ingredients.Tests;

public class IngredientsTests : IClassFixture<IngredientsApplicationFactory>, IDisposable
{
    private readonly IngredientsApplicationFactory _factory;
    private readonly IngredientsService.IngredientsServiceClient _client;

    public IngredientsTests(IngredientsApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.GetGrpcClient();
    }
    
    [Fact]
    public async Task GetsToppings()
    {
        var request = new GetToppingsRequest();
        var response = await _client.GetToppingsAsync(request);
        
        Assert.Collection(response.Toppings,
            t =>
            {
                Assert.Equal("cheese", t.Id);
            },
            t =>
            {
                Assert.Equal("tomato", t.Id);
            }
            );
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}