using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Web.GraphQl.Types
{
    public class UserEventStatusType : EnumerationGraphType<UserEventStatus>
    {
    }
}
