using Microsoft.Extensions.Configuration;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class BuiltinAvatarGraphType : ObjectGraphType<BuiltinAvatar>
    {
        public BuiltinAvatarGraphType(IConfiguration config)
        {
            Name = "Avatar";

            Field("id", avatar => avatar.AvatarId, type: typeof(IntGraphType));
            Field("filename", avatar => avatar.ImagePath, type: typeof(StringGraphType));
            Field("url", avatar => config.GetValue<string>("AssetPath") + avatar.ImagePath, type: typeof(StringGraphType));
        }
    }
}
