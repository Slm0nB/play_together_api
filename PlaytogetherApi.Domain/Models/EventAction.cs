using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlayTogetherApi.Web.Models
{
    [Flags]
    public enum EventAction
    {
        Created = 1,
        Deleted = 2,
        EditedPeriod = 4,
        EditedVisibility = 8,
        EditedText = 16,
        EditedGame = 32
    }
}
