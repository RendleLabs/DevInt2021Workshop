using Frontend.Auth;
using Grpc.Core;
using Ingredients.Protos;
using Orders.Protos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var ingredientsUri = builder.Configuration.GetServiceUri("Ingredients", "https")
                     ?? new Uri("https://localhost:5003");

builder.Services.AddGrpcClient<IngredientsService.IngredientsServiceClient>(options => { options.Address = ingredientsUri; });

var ordersUri = builder.Configuration.GetServiceUri("Orders", "https")
                ?? new Uri("https://localhost:5005");

builder.Services.AddGrpcClient<OrderService.OrderServiceClient>(options => options.Address = ordersUri)
    .ConfigureChannel((provider, channel) =>
    {
        var authHelper = provider.GetRequiredService<AuthHelper>();
        var credentials = CallCredentials.FromInterceptor(async (context, metadata) =>
        {
            var token = await authHelper.GetTokenAsync();
            metadata.Add("Authorization", $"Bearer {token}");
        });
        channel.Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, credentials);
    });

builder.Services.AddHttpClient<AuthHelper>(client =>
{
    client.BaseAddress = ordersUri;
    client.DefaultRequestVersion = new Version(2, 0);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();