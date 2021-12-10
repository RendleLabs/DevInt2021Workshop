using System.Collections.Generic;
using System.Threading;
using Grpc.Net.Client;
using Ingredients.Data;
using Ingredients.Protos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IToppingData>();

            var toppingSub = Substitute.For<IToppingData>();

            var toppings = new List<ToppingEntity>
            {
                new("cheese", "Cheese", .5d, 10),
                new("tomato", "Tomato", .5d, 10),
            };

            toppingSub.GetAsync(Arg.Any<CancellationToken>())
                .Returns(toppings);

            services.AddSingleton(toppingSub);

            services.RemoveAll<ICrustData>();

            var crustSub = Substitute.For<ICrustData>();

            var crusts = new List<CrustEntity>
            {
                new("thin", "Thin", 9, 5d, 10),
                new("classic", "Classic", 9, 5d, 10),
            };

            crustSub.GetAsync(Arg.Any<CancellationToken>())
                .Returns(crusts);

            services.AddSingleton(crustSub);
        });
        
        base.ConfigureWebHost(builder);
    }
}