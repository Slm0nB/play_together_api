using System;
using System.Text;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.Configuration;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserPreviewGraphType : ObjectGraphType<User>
    {
        public UserPreviewGraphType(PlayTogetherDbContext db, IConfiguration config)
        {
            Name = "UserPreview";

            Field("id", user => user.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");

            Field<StringGraphType>("displayNameFull",
                description: "Full Displayname.",
                resolve: context => context.Source.DisplayName + "#" + context.Source.DisplayId.ToString("D3"));

            Field(user => user.DisplayName).Description("DisplayName property from the user object.");

            Field(user => user.DisplayId).Description("DisplayId property from the user object.");

            Field(user => user.SoftDelete).Description("Indicates if this is a deleted user.");

            Field<IntGraphType>("utcOffset",
                description: "UTC offset in seconds.",
                resolve: context => context.Source.UtcOffset.HasValue ? context.Source.UtcOffset.Value.TotalSeconds : 0);

            Field<DateTimeGraphType>("temp_utcTime",
                description: "UTC timme. (this will be removed)",
                resolve: context => DateTime.UtcNow);

            Field<DateTimeGraphType>("temp_localTime",
                description: "Local time.  UTC timm + Offset. (this will be removed)",
                resolve: context => DateTime.UtcNow + ( context.Source.UtcOffset ?? TimeSpan.Zero));

            Field<StringGraphType>("avatar",
                description: "Url of the avatar image.",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "width", DefaultValue = 128 }
                ),
                resolve: context =>
                {
                    if(!string.IsNullOrWhiteSpace(context.Source.AvatarFilename))
                    {
                        // todo: handle width, or deliberately ignore?

                        var url = config.GetValue<string>("AssetPath") + context.Source.AvatarFilename;
                        return url;
                    }

                    var width = context.GetArgument<int>("width", 128);
                    var hash = md5(context.Source.Email);
                    return $"http://gravatar.com/avatar/{hash}?s={width}&d=mm";
                }
            );
        }

        private string md5(string text)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hashedBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                return hash;
            }
        }
    }
}
