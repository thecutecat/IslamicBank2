using IslamicBank.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IslamicBank.Infrastructure
{
    public class IdempotencyService:IIdempotencyService
    {
        private readonly IslamicBankDbContext _context;
        private readonly TimeSpan _defaultExpiry = TimeSpan.FromHours(24);

        public IdempotencyService(IslamicBankDbContext context)
        {
            _context = context;
        }



        public async Task<bool> RequestExistsAsync(string key)
        {
            return await _context.IdempotencyRecords.AnyAsync(r => r.Key == key); 
        }

        public async Task<T> GetCachedResponseAsync<T>(string key)
        {
            var record = await _context.IdempotencyRecords.FindAsync(key);
            if (record == null) return default;

            return JsonSerializer.Deserialize<T>(record.Response);
        }

        public async Task CacheResponseAsync<T>(string key, T response, TimeSpan? expiry = null)
        {
            var record = new IdempotencyRecord
            {
                Key = key,
                Response = JsonSerializer.Serialize(response),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiry ?? _defaultExpiry)
            };

            await _context.IdempotencyRecords.AddAsync(record);
            await _context.SaveChangesAsync();
        }
    }

    public interface IIdempotencyService
    {
        Task<bool> RequestExistsAsync(string key);
        Task<T> GetCachedResponseAsync<T>(string key);
        Task CacheResponseAsync<T>(string key, T response, TimeSpan? expiry = null);
    }
}
