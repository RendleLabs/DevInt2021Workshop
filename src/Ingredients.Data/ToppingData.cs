using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Ingredients.Data;

public class ToppingData : IToppingData
{
    private readonly ILogger<ToppingData> _log;
    private const string TableName = "toppings";
    private readonly TableClient _client;
    private readonly SemaphoreSlim _semaphore = new(1);
    private bool _initialized;

    public ToppingData(ILogger<ToppingData> log)
    {
        _log = log;
        _client = new TableClient(Constants.StorageConnectionString, TableName);
    }

    public async Task<List<ToppingEntity>> GetAsync(CancellationToken token = default)
    {
        await EnsureInitialized();
        
        try
        {
            return await _client.QueryAsync<ToppingEntity>(cancellationToken: token).ToListAsync(token);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error reading data.");
            throw;
        }
    }

    public async Task DecrementStockAsync(string id, CancellationToken token = default)
    {
        await EnsureInitialized();
        
        for (int i = 0; i < 100; i++)
        {
            ToppingEntity entity;
            try
            {
                var response = await _client.GetEntityAsync<ToppingEntity>("topping", id, cancellationToken: token);
                entity = response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _log.LogError(ex, "Data not found");
                return;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error retrieving data.");
                throw;
            }

            if (entity.StockCount == 0) return;
            entity.StockCount--;
            try
            {
                await _client.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, token);
                break;
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                _log.LogInformation("Conflict updating entity, retrying.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error updating data.");
                throw;
            }
        }
    }

    private ValueTask EnsureInitialized()
    {
        return _initialized ? default : new ValueTask(InitializeAsync());
    }

    private async Task InitializeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_initialized) return;

            _initialized = true;

            var response = await _client.CreateIfNotExistsAsync();
            
            if (response is null) return;

            await Task.WhenAll(
                AddAsync("cheese", "Cheese", 0.5d, 10000),
                AddAsync("sauce", "Tomato Sauce", 0.5d, 10000),
                AddAsync("pepperoni", "Pepperoni", 1d, 1000),
                AddAsync("ham", "Ham", 1d, 1000),
                AddAsync("mushroom", "Mushrooms", 0.75d, 1000),
                AddAsync("pineapple", "Pineapple", 2d, 1000),
                AddAsync("anchovies", "Anchovies", 1d, 1000),
                AddAsync("peppers", "Peppers", 0.75d, 1000),
                AddAsync("onion", "Onion", 0.75d, 1000),
                AddAsync("olives", "Olives", 1d, 1000),
                AddAsync("beef", "Beef", 1d, 1000)
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error initializing Crust data");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task AddAsync(string id, string name, double price, int stockCount)
    {
        try
        {
            var entity = new ToppingEntity(id, name, price, stockCount);
            await _client.AddEntityAsync(entity);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error inserting data.");
            throw;
        }
    }
}
