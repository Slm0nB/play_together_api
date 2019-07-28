using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using GameCalendarApi.Web.Models;

namespace GameCalendarApi.Web.GraphQl.Types
{
    public class TokenResponseType : ObjectGraphType<TokenResponseModel>
    {
        public TokenResponseType()
        {
            Name = "TokenResponse";
            Field(x => x.Access_token, type: typeof(StringGraphType), nullable: false);
            Field(x => x.Refresh_token, type: typeof(StringGraphType), nullable: false);
            Field(x => x.Token_type, type: typeof(StringGraphType), nullable: false);
            Field(x => x.Expires_in, type: typeof(IntGraphType));
        }
    }
}
