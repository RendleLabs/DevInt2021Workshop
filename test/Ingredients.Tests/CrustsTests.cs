using System;
using System.Threading.Tasks;
using Ingredients.Protos;
using Xunit;

namespace Ingredients.Tests;

public class CrustsTests : IClassFixture<IngredientsApplicationFactory>, IDisposable
{
    private readonly IngredientsApplicationFactory _factory;
    private readonly IngredientsService.IngredientsServiceClient _client;

    public CrustsTests(IngredientsApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.GetGrpcClient();
    }
    
    [Fact]
    public async Task GetsCrusts()
    {
        var request = new GetCrustsRequest();
        var response = await _client.GetCrustsAsync(request);
        
        Assert.Collection(response.Crusts,
            t =>
            {
                Assert.Equal("thin", t.Id);
            },
            t =>
            {
                Assert.Equal("classic", t.Id);
            }
        );
    }

    public void Dispose()
    {
    }
}