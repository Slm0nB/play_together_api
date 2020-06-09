using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Services
{
    public class InteractionsService
    {
        public bool EnableStatistics = true;
        public bool EnablePushMessages = true;

        PlayTogetherDbContext db;
        ObservablesService observables;
        PushMessageService pushMessageService;
        FriendLogicService friendLogicService;
        UserStatisticsService userStatisticsService;

        public InteractionsService(
            PlayTogetherDbContext db,
            ObservablesService observables,
            PushMessageService pushMessageService,
            FriendLogicService friendLogicService,
            UserStatisticsService userStatisticsService)
        {
            this.db = db;
            this.observables = observables;
            this.pushMessageService = pushMessageService;
            this.friendLogicService = friendLogicService;
            this.userStatisticsService = userStatisticsService;
        }

        #region Events

        public async Task<Event> CreateEventAsync(Guid callingUserId, DateTime startDate, DateTime endDate, string title, string description, bool friendsOnly, Guid gameId)
        {
            var callingUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == callingUserId);
            if (callingUser == null)
            {
                throw new Exception("User not found.");
            }

            if (startDate > endDate)
            {
                throw new Exception("Start- and end-dates not in correct order.");
            }

            if (!await db.Games.AnyAsync(n => n.GameId == gameId))
            {
                throw new Exception("Game doesn't exist.");
            }

            var newEvent = new Event
            {
                Title = title,
                CreatedByUserId = callingUserId,
                EventDate = startDate,
                EventEndDate = endDate,
                Description = description,
                FriendsOnly = friendsOnly,
                GameId = gameId
            };
            db.Events.Add(newEvent);
            await db.SaveChangesAsync();

            var friends = await db.UserRelations.Where(n => n.Status == FriendLogicService.Relation_MutualFriends && (n.UserAId == callingUser.UserId || n.UserBId == callingUser.UserId)).ToArrayAsync();
            var friendIds = friends.Select(n => n.UserAId == callingUserId ? n.UserBId : n.UserAId).ToList();
            var friendDeviceTokens = await db.Users.Where(n => friendIds.Contains(n.UserId)).Select(n => n.DeviceToken).ToArrayAsync();

            observables.GameEventStream.OnNext(new EventChangedModel
            {
                Event = newEvent,
                ChangingUser = callingUser,
                FriendsOfChangingUser = !newEvent.FriendsOnly ? null : friends,
                Action = EventAction.Created,
            }); ;

            var signup = new UserEventSignup
            {
                EventId = newEvent.EventId,
                UserId = callingUserId,
                Status = UserEventStatus.AcceptedInvitation
            };
            db.UserEventSignups.Add(signup);
            await db.SaveChangesAsync();

            observables.UserEventSignupStream.OnNext(signup);

            if (EnablePushMessages && pushMessageService != null)
            {
                //foreach (var email in friendEmails)
                {
                    _ = pushMessageService.PushMessageAsync(
                        "FriendEvent",
                        $"{callingUser.DisplayName} created an event.",
                        newEvent.Title,
                        new
                        {
                            type = "FriendEvent",
                            userId = callingUserId.ToString("N"),
                            userName = callingUser.DisplayName,
                            eventId = newEvent.EventId.ToString("N"),
                            eventTitle = newEvent.Title
                        },
                        friendDeviceTokens
                    );
                }
            }

            if (EnableStatistics && userStatisticsService != null)
            {
                await userStatisticsService.UpdateStatisticsAsync(db, callingUserId, callingUser);
            }

            return newEvent;
        }

        #endregion

        #region User relations

        public async Task<UserRelationExtModel> ChangeUserRelationAsync(Guid callingUserId, Guid friendUserId, UserRelationAction action)
        {
            var callingUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == callingUserId);
            if (callingUser == null)
            {
                throw new Exception("Calling user not found.");
            }

            var friendUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == friendUserId);
            if (friendUser == null)
            {
                throw new Exception("User not found.");
            }

            var relation = await db.UserRelations.FirstOrDefaultAsync(n => (n.UserAId == callingUserId && n.UserBId == friendUserId) || (n.UserAId == friendUserId && n.UserBId == callingUserId));

            var previousStatusForFriendUser = relation == null
                ? UserRelationStatus.None
                : friendLogicService.GetStatusForUser(relation, friendUserId);

            if (relation == null)
            {
                // Inviting (or blocking) an unrelated user
                if (action != UserRelationAction.Invite && action != UserRelationAction.Block)
                {
                    throw new Exception("Can only invite or block unrelated users.");
                }
                relation = new UserRelation
                {
                    UserAId = callingUserId,
                    UserBId = friendUserId,
                    Status = action == UserRelationAction.Invite ? UserRelationInternalStatus.A_Invited : UserRelationInternalStatus.A_Blocked,
                    CreatedDate = DateTime.Now
                };
                db.UserRelations.Add(relation);
            }
            else
            {
                var newStatus = friendLogicService.GetUpdatedStatus(relation, callingUserId, action);
                relation.Status = newStatus;
            }
            await db.SaveChangesAsync();

            if (EnablePushMessages && pushMessageService != null)
            {
                if (friendLogicService.GetStatusForUser(relation, callingUserId) == UserRelationStatus.Inviting)
                {
                    var _ = pushMessageService.PushMessageAsync(
                        "InviteFriend",
                        "You have a friend-request!",
                        $"{callingUser.DisplayName} wants to be your friend.",
                        new
                        {
                            type = "InviteFriend",
                            userId = callingUserId,
                            userName = callingUser.DisplayName
                        },
                        friendUser.DeviceToken
                    );
                }
            }

            var observableModel = new UserRelationChangedModel
            {
                Relation = relation,
                ActiveUser = callingUser,
                ActiveUserAction = action,
                TargetUser = friendUser
            };
            observables.UserRelationChangeStream.OnNext(observableModel);

            if (EnableStatistics && userStatisticsService != null)
            {
                await userStatisticsService.UpdateStatisticsAsync(db, callingUser.UserId, callingUser);
                await userStatisticsService.UpdateStatisticsAsync(db, friendUser.UserId, friendUser);
            }



            // todo: if the users became friends, then send each of them VISIBLE-subscription for private-only events created by the other user
            // todo: if the users stopped being friends, then send each of them NOT VISIBLE-subscriptions for private-only events created by the other user




            var result = new UserRelationExtModel
            {
                Relation = relation,
                ActiveUserId = callingUserId,
                ActiveUserAction = action,
                PreviousStatusForTargetUser = previousStatusForFriendUser
            };
            return result;
        }

        #endregion
    }
}
