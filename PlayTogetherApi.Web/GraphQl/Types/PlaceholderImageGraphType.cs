using Microsoft.Extensions.Configuration;
using GraphQL.Types;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class PlaceholderImageGraphType : ObjectGraphType<PlaceholderImage>
    {
        public PlaceholderImageGraphType(IConfiguration config)
        {
            Name = "PlaceholderImage";

            Field("id", placeholderImage => placeholderImage.ImageId, type: typeof(IntGraphType));
            Field("filename", placeholderImage => placeholderImage.ImagePath, type: typeof(StringGraphType));
            Field("url", placeholderImage => config.GetValue<string>("AssetPath") + placeholderImage.ImagePath, type: typeof(StringGraphType));
            Field<StringGraphType>("category", resolve: context => context.Source.Category.ToString() );
        }
    }
}
