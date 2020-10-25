using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;

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
                    var db = context.RequestServices.GetService<PlayTogetherDbContext>();
                    var connectionString = db.Database.GetDbConnection().ConnectionString;
                    var dbName = connectionString?.Split(';').FirstOrDefault(n => n.StartsWith(prefix));
                    return dbName?.Substring(prefix.Length);
                });
        }
    }
}
