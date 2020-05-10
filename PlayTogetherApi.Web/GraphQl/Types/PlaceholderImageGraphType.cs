using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GraphQL.Types;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

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
