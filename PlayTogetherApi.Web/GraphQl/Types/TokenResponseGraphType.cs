using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class TokenResponseGraphType : ObjectGraphType<TokenResponseModel>
    {
        public TokenResponseGraphType()
        {
            Name = "TokenResponse";

            Field(x => x.Access_token, type: typeof(NonNullGraphType<StringGraphType>), nullable: false);
            Field(x => x.Refresh_token, type: typeof(StringGraphType), nullable: true);
            Field(x => x.Token_type, type: typeof(StringGraphType), nullable: false);
            Field(x => x.Expires_in, type: typeof(NonNullGraphType<IntGraphType>));
        }
    }
}
