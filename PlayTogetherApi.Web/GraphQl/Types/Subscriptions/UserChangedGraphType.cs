using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserChangedGraphType : ObjectGraphType<UserChangedSubscriptionModel>
    {
        public UserChangedGraphType()
        {
            Name = "UserChanged";

            Field<UserPreviewGraphType>("user",
                resolve: context => context.Source.ChangingUser
            );
        }
    }
}
