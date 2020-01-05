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
        EditedPeriod = 2,
        EditedVisibility = 4,
        EditedText = 8,
        EditedGame = 16
    }
}
