namespace IslamicBank.Services
{
    public class ProfitDistributionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProfitDistributionBackgroundService> _logger;

        public ProfitDistributionBackgroundService(IServiceProvider serviceProvider, ILogger<ProfitDistributionBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextDistribution = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                var delay = nextDistribution - now;

                await Task.Delay(delay, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var profitService = scope.ServiceProvider.GetRequiredService<IProfitDistributionService>();

                try
                {
                    _logger.LogInformation("Running monthly profit distribution for {Month}", nextDistribution.AddMonths(-1));
                    await profitService.DistributeMonthlyProfitsAsync(nextDistribution.AddDays(-1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Profit distribution failed");
                }
            }
        }
    }
}
