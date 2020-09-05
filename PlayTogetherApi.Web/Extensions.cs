using System;
using System.Linq;
using System.Security.Claims;
using GraphQL;
using GraphQL.Types;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.GraphQl;

namespace PlayTogetherApi.Web
{
    public static class Extensions
    {
        public static Guid GetClaimedUserId<T>(this IResolveFieldContext<T> context)
        {
            var userContext = context.UserContext as PlayTogetherUserContext;
            var userIdClaim = userContext.User.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new Exception("Unauthorized");
            }
            return userId;
        }

        public static bool TryGetClaimedUserId<T>(this IResolveFieldContext<T> context, out Guid userId)
        {
            var userContext = context.UserContext as PlayTogetherUserContext;
            var userIdClaim = userContext.User.Claims.FirstOrDefault(n => n.Type == "userid")?.Value;
            return Guid.TryParse(userIdClaim, out userId);
        }

        public static void ReadParametersFromContext(this EventsQueryService queryService, IResolveFieldContext context)
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
