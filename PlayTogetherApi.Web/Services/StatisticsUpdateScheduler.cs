using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Web.Services
{
    public class StatisticsUpdateScheduler : IHostedService, IDisposable
    {
        private IServiceProvider _serviceProvider;
        private UserStatisticsService _userStatisticsService;
        private Timer _timer;
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public StatisticsUpdateScheduler(IServiceProvider serviceProvider, UserStatisticsService userStatisticsService)
        {
            _serviceProvider = serviceProvider;
            _userStatisticsService = userStatisticsService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(_ => Task.Run(() => DoWorkAsync()), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        private async Task DoWorkAsync()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                using (var scope = _serviceProvider.CreateScope())
                {
                    using (var db = scope.ServiceProvider.GetRequiredService<PlayTogetherDbContext>())
                    {
                        await _userStatisticsService.UpdateExpiredStatisticsAsync(db);
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
