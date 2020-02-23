using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserPreviewGraphType : ObjectGraphType<User>
    {
        public UserPreviewGraphType(PlayTogetherDbContext db, IConfiguration config)
        {
            Name = "UserPreview";

            Field("id", user => user.UserId, type: typeof(IdGraphType)).Description("Id property from the user object.");
            Field<StringGraphType>("displayNameFull", resolve: context => context.Source.DisplayName + "#" + context.Source.DisplayId.ToString("D3"), description: "Full Displayname.");
            Field(user => user.DisplayName).Description("DisplayName property from the user object.");
            Field(user => user.DisplayId).Description("DisplayId property from the user object.");

            Field<StringGraphType>("avatar",
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
                },
                description: "Url of the avatar image."
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
