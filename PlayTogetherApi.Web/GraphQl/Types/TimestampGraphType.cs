using System;
using System.Globalization;
using GraphQL;
using GraphQL.Types;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class TimestampGraphType : ObjectGraphType<TimestampModel>
    {
        public TimestampGraphType()
        {
            Name = "Timestamp";

            Field<DateTimeGraphType>("datetime", resolve: context => context.Source.DateTime);

            Field<IntGraphType>("ticks", resolve: context => context.Source.DateTime.Ticks);

            Field<StringGraphType>("format",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "format", Description = ".NET Datetime formatting string.", DefaultValue = "MMMM dd, yyyy - HH:mm:ss" }
                ),
                resolve: context =>
                {
                    var format = context.GetArgument<string>("format", "MMMM dd, yyyy - HH:mm:ss");
                    return context.Source.DateTime.ToString(format);
                }
            );

            Field<StringGraphType>("text", resolve: context => context.Source.DateTime.ToString(CultureInfo.InvariantCulture));
        }
    }
}
