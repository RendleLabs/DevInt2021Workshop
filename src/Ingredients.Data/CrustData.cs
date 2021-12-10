using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Ingredients.Data;

public class CrustData : ICrustData
{
    private readonly ILogger<CrustData> _log;
    private const string TableName = "crusts";
    private readonly TableClient _client;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
    private bool _initialized;

    public CrustData(ILogger<CrustData> log)
    {
        _log = log;
        _client = new TableClient(Constants.StorageConnectionString, TableName);
    }

    public async Task<List<CrustEntity>> GetAsync(CancellationToken token = default)
    {
        await EnsureInitialized();
        
        try
        {
            return await _client.QueryAsync<CrustEntity>().ToListAsync(token);
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
            var response = await _client.GetEntityAsync<CrustEntity>("crust", id, cancellationToken: token);
            var entity = response.Value;
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
            
            await AddAsync("thin9", "Thin", 9, 5d, 1000);
            await AddAsync("thin12", "Thin", 12, 7.50d, 1000);
            await AddAsync("thin15", "Thin", 15, 10d, 1000);
            await AddAsync("deep9", "Deep", 9, 6d, 1000);
            await AddAsync("deep12", "Deep", 12, 9d, 1000);
            await AddAsync("deep15", "Deep", 15, 12d, 1000);
            await AddAsync("stuffed12", "Stuffed", 12, 10d, 1000);
            await AddAsync("stuffed15", "Stuffed", 15, 14d, 1000);
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

    private async Task AddAsync(string id, string name, int size, double price, int stockCount)
    {
        try
        {
            var entity = new CrustEntity(id, name, size, price, stockCount);
            await _client.AddEntityAsync(entity);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error inserting data.");
            throw;
        }
    }
}
