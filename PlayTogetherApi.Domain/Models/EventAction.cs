using System;

namespace PlayTogetherApi.Models
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
