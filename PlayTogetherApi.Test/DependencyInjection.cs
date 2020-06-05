using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

namespace PlayTogetherApi.Test
{
    public class DependencyInjection
    {
        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<ObservablesService>();
            services.AddSingleton<FriendLogicService>();
            services.AddSingleton<UserStatisticsService>();

            services.AddDbContext<PlayTogetherDbContext>(opt =>
            {
                opt.UseInMemoryDatabase();
            });

            return services.BuildServiceProvider();
        }
    }
}
