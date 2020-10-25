using System.Linq;
using GraphQL.Types;
using PlayTogetherApi.Services;
using Microsoft.Extensions.Configuration;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class SystemGraphType : ObjectGraphType
    {
        public SystemGraphType(ObservablesService observablesService, IConfiguration configuration)
        {
            Name = "System";

            Field<NonNullGraphType<IntGraphType>>("userStatisticsSubscriptionCount",
                resolve: context =>
                {
                    return observablesService.UserStatisticsStreams.Count();
                });

            Field<NonNullGraphType<StringGraphType>>("version",
                resolve: context =>
                {
                    return System.Reflection.Assembly.GetExecutingAssembly().FullName;
                });


            Field<NonNullGraphType<StringGraphType>>("db",
                resolve: context =>
                {
                    var prefix = "Database=";
                    var connectionString = configuration.GetSection("PlayTogetherConnectionString")?.Value;
                    var dbName = connectionString?.Split(';').FirstOrDefault(n => n.StartsWith(prefix));
                    return dbName?.Substring(prefix.Length);
                });
        }
    }
}
