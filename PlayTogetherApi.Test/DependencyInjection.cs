using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Test
{
    public sealed class DependencyInjection : IDisposable
    {
        IServiceProvider serviceProvider;
        IServiceScope scope;

        public DependencyInjection()
        {
            serviceProvider = ConfigureServices();
        }

        public T GetService<T>()
        {
            scope = scope ?? serviceProvider.CreateScope();
            return scope.ServiceProvider.GetService<T>();
        }

        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ObservablesService>();
            services.AddSingleton<FriendLogicService>();
            services.AddSingleton<UserStatisticsService>();

            services.AddScoped<InteractionsService>();

            services.AddDbContext<PlayTogetherDbContext>(opt =>
            {
                opt.UseInMemoryDatabase();
            });

            return services.BuildServiceProvider();
        }

        public void Dispose()
        {
            scope?.Dispose();
            scope = null;
        }
    }
}
