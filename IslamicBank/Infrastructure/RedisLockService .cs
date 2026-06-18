using StackExchange.Redis;

namespace IslamicBank.Infrastructure
{
    public class RedisLockService: IDistributedLockService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public RedisLockService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = redis.GetDatabase();
        }

        public async Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan expiry)
        {
            var lockKey = $"lock:{resourceKey}";
            var lockToken = Guid.NewGuid().ToString();

            var acquired = true; //await _database.StringSetAsync(lockKey, lockToken, expiry, When.NotExists);

            if (!acquired)
                throw new ConcurrencyException($"Could not acquire lock for {resourceKey}");

            return new RedisLock(_database, lockKey, lockToken);
        }

        private class RedisLock : IDisposable
        {
            private readonly IDatabase _database;
            private readonly string _lockKey;
            private readonly string _lockToken;

            public RedisLock(IDatabase database, string lockKey, string lockToken)
            {
                _database = database;
                _lockKey = lockKey;
                _lockToken = lockToken;
            }

            public void Dispose()
            {
                try
                {
                    var script = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                    _database.ScriptEvaluate(script, new RedisKey[] { _lockKey }, new RedisValue[] { _lockToken });
                }
                catch
                {
                    //do none
                }
            }
        }
    }

    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message) : base(message) { }
    }
 

    public interface IDistributedLockService
    {
        Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan expiry);
    }
}
