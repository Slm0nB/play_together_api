using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class SelfUserType : UserType
    {
        public SelfUserType(PlayTogetherDbContext db, IConfiguration config) : base(db, config)
        {
            Name = "SelfUser";

            Field(user => user.Email).Description("Email property from the user object.");
        }
    }
}
