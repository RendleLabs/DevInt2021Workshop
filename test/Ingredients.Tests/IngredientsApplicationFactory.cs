using Grpc.Net.Client;
using Ingredients.Protos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ingredients.Tests;

public class IngredientsApplicationFactory : WebApplicationFactory<IngredientsMarker>
{
    public IngredientsService.IngredientsServiceClient GetGrpcClient()
    {
        var http = CreateDefaultClient();
        var channel = GrpcChannel.ForAddress(http.BaseAddress, new GrpcChannelOptions
        {
            HttpClient = http
        });
        return new IngredientsService.IngredientsServiceClient(channel);
    }
}