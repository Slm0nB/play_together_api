using System;
using GraphQL.Types;
using PlayTogetherApi.Web.Services;

namespace PlayTogetherApi.Web
{
    public static class DomainExtensions
    {
        public static void ReadParametersFromContext(this EventsQueryService queryService, ResolveFieldContext<object> context)
        {
            queryService.SearchTerm = context.GetArgument<string>("search");

            queryService.StartsBeforeDate = context.HasArgument("startsBeforeDate") ? context.GetArgument<DateTime>("startsBeforeDate") : (DateTime?)null;
            queryService.StartsAfterDate = context.HasArgument("startsAfterDate") ? context.GetArgument<DateTime>("startsAfterDate") : (DateTime?)null;
            queryService.EndsBeforeDate = context.HasArgument("endsBeforeDate") ? context.GetArgument<DateTime>("endsBeforeDate") : (DateTime?)null;
            queryService.EndsAfterDate = context.HasArgument("endsAfterDate") ? context.GetArgument<DateTime>("endsAfterDate") : (DateTime?)null;

            queryService.OnlyPrivateFilter = context.HasArgument("onlyPrivate") ? context.GetArgument<bool>("onlyPrivate") : false;
            queryService.OnlyByFriendsFilter = context.HasArgument("onlyByFriends") ? context.GetArgument<bool>("onlyByFriends") : false;
            queryService.OnlyJoinedFilter = context.HasArgument("onlyJoined") ? context.GetArgument<bool>("onlyJoined") : false;
            queryService.OnlyJoinedByFriendsFilter = context.HasArgument("onlyJoinedByFriends") ? context.GetArgument<bool>("onlyJoinedByFriends") : false;
            queryService.OnlyByUsersFilter = context.HasArgument("onlyByUsers") ? context.GetArgument<Guid[]>("onlyByUsers") : null;
            queryService.OnlyGamesFilter = context.HasArgument("onlyGames") ? context.GetArgument<Guid[]>("onlyGames") : null;

            queryService.IncludePrivateFilter = context.HasArgument("includePrivate") ? context.GetArgument<bool>("includePrivate") : false;
            queryService.IncludeByFriendsFilter = context.HasArgument("includeByFriends") ? context.GetArgument<bool>("includeByFriends") : false;
            queryService.IncludeJoinedFilter = context.HasArgument("includeJoined") ? context.GetArgument<bool>("includeJoined") : false;
            queryService.IncludeJoinedByFriendsFilter = context.HasArgument("includeJoinedByFriends") ? context.GetArgument<bool>("includeJoinedByFriends") : false;
            queryService.IncludeByUsersFilter = context.HasArgument("includeByUsers") ? context.GetArgument<Guid[]>("includeByUsers") : null;
            queryService.IncludeGamesFilter = context.HasArgument("includeGames") ? context.GetArgument<Guid[]>("includeGames") : null;
        }
    }
}
