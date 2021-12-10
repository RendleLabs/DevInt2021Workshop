
using System.Security.Claims;
using AuthHelp;
using Ingredients.Protos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Orders.PubSub;
using Orders.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

var ingredientsUri = builder.Configuration.GetServiceUri("ingredients")
                     ?? new Uri("http://localhost:5003");

builder.Services.AddGrpcClient<IngredientsService.IngredientsServiceClient>(options =>
{
    options.Address = ingredientsUri;
});

builder.Services.AddOrderPubSub();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateActor = false,
            ValidateLifetime = true,
            IssuerSigningKey = JwtHelper.SecurityKey
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireClaim(ClaimTypes.Name);
    });
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.MapGrpcService<OrderServiceImpl>();

app.MapGet("/generateJwtToken", context =>
    context.Response.WriteAsync(JwtHelper.GenerateJwtToken(context.Request.Query["name"])));
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
