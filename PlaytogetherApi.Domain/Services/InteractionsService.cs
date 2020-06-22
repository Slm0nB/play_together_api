#define EXTRA_EVENTS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Data;
using PlayTogetherApi.Web.Models;

namespace PlayTogetherApi.Services
{
    /// <summary>
    /// This implements the operations in the API, divorced from matters like deserializing input etc.
    /// </summary>
    public class InteractionsService
    {
        public bool EnableStatistics = true;
        public bool EnablePushMessages = true;

        PlayTogetherDbContext db;
        ObservablesService observables;
        PushMessageService pushMessageService;
        FriendLogicService friendLogicService;
        UserStatisticsService userStatisticsService;
        PasswordService passwordService;

        public InteractionsService(
            PlayTogetherDbContext db,
            PasswordService passwordService,
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
            this.passwordService = passwordService;
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

            var signup = new UserEventSignup
            {
                EventId = newEvent.EventId,
                UserId = callingUserId,
                Status = UserEventStatus.AcceptedInvitation
            };
            db.UserEventSignups.Add(signup);

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
            });

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

        public async Task<Event> CallToArmsAsync(Guid callingUserId, DateTime startDate, string title, Guid gameId)
        {
            var callingUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == callingUserId);
            if (callingUser == null)
            {
                throw new Exception("User not found.");
            }

            if (startDate > DateTime.Now.AddMinutes(90))
            {
                throw new Exception("Startdate must be within 90 minutes.");
            }

            if (gameId != default(Guid))
            {
                if (!await db.Games.AnyAsync(n => n.GameId == gameId))
                {
                    throw new Exception("Game doesn't exist.");
                }
            }

            var friendsOfChangingUser = await db.UserRelations.Where(n => n.Status == FriendLogicService.Relation_MutualFriends && (n.UserAId == callingUser.UserId || n.UserBId == callingUser.UserId)).ToArrayAsync();
            if (friendsOfChangingUser.Length == 0)
            {
                throw new Exception("Must have friends to issue a call to arms :(");
            }

            var newEvent = new Event
            {
                Title = title,
                CreatedByUserId = callingUserId,
                EventDate = startDate,
                EventEndDate = startDate + TimeSpan.FromMinutes(45),
                Description = "",
                FriendsOnly = true,
                CallToArms = true,
                GameId = gameId
            };
            db.Events.Add(newEvent);
            await db.SaveChangesAsync();

            observables.GameEventStream.OnNext(new EventChangedModel
            {
                Event = newEvent,
                ChangingUser = callingUser,
                FriendsOfChangingUser = friendsOfChangingUser,
                Action = EventAction.Created,
            });

            var friendIds = friendsOfChangingUser.Select(n => n.UserAId == callingUserId ? n.UserBId : n.UserAId).ToList();
            var friendDeviceTokens = await db.Users.Where(n => friendIds.Contains(n.UserId)).Select(n => n.DeviceToken).ToArrayAsync();

            await userStatisticsService.UpdateStatisticsAsync(db, callingUserId, callingUser);
            foreach (var friendId in friendIds)
            {
                await userStatisticsService.UpdateStatisticsAsync(db, friendId);
            }

            _ = pushMessageService.PushMessageAsync(
                "CallToArms",
                $"{callingUser.DisplayName} calls you to arms!",
                newEvent.Title,
                new
                {
                    type = "CallToArms",
                    userId = callingUserId.ToString("N"),
                    userName = callingUser.DisplayName,
                    eventId = newEvent.EventId.ToString("N"),
                    eventTitle = newEvent.Title
                },
                friendDeviceTokens
            );

            return newEvent;
        }

        public async Task<Event> UpdateEventAsync(Guid userId, Guid eventId, DateTime? startDate = null, DateTime? endDate = null, string title = null, string description = null, bool? friendsOnly = null, Guid? gameId = null)
        {
            // todo: if this is a call to arms, it should reject changing friendonly as well as the date

            EventAction? action = null;

            var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var editedEvent = await db.Events.Include(n => n.Signups).FirstOrDefaultAsync(n => n.EventId == eventId);
            if (editedEvent == null || editedEvent.CreatedByUserId != userId)
            {
                throw new Exception("Must be the creator of the event to modify it.");
            }

            if (startDate.HasValue && startDate != default)
            {
                if (editedEvent.CallToArms && startDate > DateTime.Now.AddMinutes(60)) // todo: UtcNow ?
                {
                    throw new Exception("Call to arms must be within 60 minutes.");
                }

                editedEvent.EventDate = startDate.Value;
                action = EventAction.EditedPeriod;
            }

            if (endDate.HasValue && endDate != default)
            {
                if (editedEvent.CallToArms && endDate > DateTime.Now.AddHours(4)) // todo: UtcNow ?
                {
                    throw new Exception("Call to arms can't end more then 4 hours in the future.");
                }

                editedEvent.EventEndDate = endDate.Value;
                action = EventAction.EditedPeriod;
            }

            if (editedEvent.EventDate > editedEvent.EventEndDate)
            {
                throw new Exception("Start- and end-dates not in correct order.");
            }

            if (!string.IsNullOrEmpty(title))
            {
                editedEvent.Title = title;
                action = action.HasValue ? (action.Value | EventAction.EditedText) : EventAction.EditedText;
            }

            if (!string.IsNullOrEmpty(description))
            {
                editedEvent.Description = description;
                action = action.HasValue ? (action.Value | EventAction.EditedText) : EventAction.EditedText;
            }

            if (friendsOnly.HasValue)
            {
                if (editedEvent.CallToArms)
                {
                    throw new Exception("Can't change the friendsonly-state of a call to arms.");
                }

                if (friendsOnly != editedEvent.FriendsOnly)
                {
                    editedEvent.FriendsOnly = friendsOnly.Value;
                    action = action.HasValue ? (action.Value | EventAction.EditedVisibility) : EventAction.EditedVisibility;
                }
            }

            if (gameId.HasValue && gameId != default)
            {
                if (!await db.Games.AnyAsync(n => n.GameId == gameId))
                {
                    throw new Exception("Game doesn't exist.");
                }

                editedEvent.GameId = gameId.Value;
                action = action.HasValue ? (action.Value | EventAction.EditedGame) : EventAction.EditedGame;
            }

            db.Events.Update(editedEvent);
            await db.SaveChangesAsync();

            observables.GameEventStream.OnNext(new EventChangedModel
            {
                Event = editedEvent,
                ChangingUser = user,
                FriendsOfChangingUser = !editedEvent.FriendsOnly ? null : await db.UserRelations.Where(n => n.Status == FriendLogicService.Relation_MutualFriends && (n.UserAId == user.UserId || n.UserBId == user.UserId)).ToArrayAsync(),
                Action = action
            });

            if (action.HasValue && action.Value.HasFlag(EventAction.EditedPeriod))
            {
                await userStatisticsService.UpdateStatisticsAsync(db, userId, user);
            }

            return editedEvent;
        }

        public async Task<Event> DeleteEventAsync(Guid callingUserId, Guid eventId)
        {
            var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == callingUserId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var dbEvent = await db.Events.FirstOrDefaultAsync(n => n.EventId == eventId);
            if (dbEvent == null)
            {
                throw new Exception("Event doesn't exist.");
            }
            if (dbEvent.CreatedByUserId != callingUserId)
            {
                throw new Exception("Event not created by user.");
            }

            db.Events.Remove(dbEvent);
            await db.SaveChangesAsync();

            observables.GameEventStream.OnNext(new EventChangedModel
            {
                Event = dbEvent,
                ChangingUser = user,
                Action = EventAction.Deleted
            });

            await userStatisticsService.UpdateStatisticsAsync(db, callingUserId, user);

            return dbEvent;
        }

        #endregion

        #region User-event signups

        /// <summary>
        /// Modify a signup to an event.
        /// This validates that the change is done either by the participant, or the owner of the event.
        /// </summary>
        /// <param name="userId">The user to modify the signup for.</param>
        /// <param name="authenticatedUserId">The user making the request (the same user, or the creator of the event).</param>
        public async Task<UserEventSignup> UpdateSignupAsync(Guid userId, Guid eventId, UserEventStatus status, Guid authenticatedUserId)
        {
            var eventDto = await db.Events.FirstOrDefaultAsync(n => n.EventId == eventId);
            if (eventDto == null)
            {
                throw new Exception("Event doesn't exist.");
            }

            var signup = await db.UserEventSignups.FirstOrDefaultAsync(n => n.EventId == eventId && n.UserId == userId);
            if (signup == null)
            {
                throw new Exception("No signup found.");
            }

            if (userId != authenticatedUserId && eventDto.CreatedByUserId != authenticatedUserId)
            {
                throw new Exception("Must be the creator of the event to modify other users signups.");
            }

            signup.Status = status;

            db.UserEventSignups.Update(signup);
            await db.SaveChangesAsync();

            observables.UserEventSignupStream.OnNext(signup);

            await userStatisticsService.UpdateStatisticsAsync(db, userId);

            // todo: if the new status was AcceptedInvitation, maybe push a message to the owner of the event (if this was done by the participant), or to the participant (if this was done by the owner of the event)

            return signup;
        }

        public async Task<UserEventSignup> JoinEventAsync(Guid callingUserId, Guid eventId, UserEventStatus status = UserEventStatus.AcceptedInvitation)
        {
            UserEventSignup signup = null;

            var callingUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == callingUserId);
            if (callingUser == null)
            {
                throw new Exception("User not found.");
            }

            var gameEvent = await db.Events.FirstOrDefaultAsync(n => n.EventId == eventId);
            if (gameEvent == null)
            {
                throw new Exception("Event not found.");
            }

            var eventOwner = await db.Users.FirstOrDefaultAsync(n => n.UserId == gameEvent.CreatedByUserId);
            if (eventOwner == null)
            {
                throw new Exception("Invalid event; the creator no longer exists.");
            }

            if (gameEvent.FriendsOnly)
            {
                // todo: if it's a friendsonly event, then verify that the caller is the creator or a friend of the creator
            }

            signup = await db.UserEventSignups.FirstOrDefaultAsync(n => n.EventId == eventId && n.UserId == callingUserId);
            if (signup == null)
            {
                signup = new UserEventSignup
                {
                    EventId = eventId,
                    UserId = callingUserId
                };
                db.UserEventSignups.Add(signup);
            }
            else
            {
                // todo: forbid setting it to accepted, if the status is about being rejected by the event-owner

                if (signup.Status == UserEventStatus.AcceptedInvitation)
                    throw new Exception("Already signed up to this event.");
            }

            signup.Status = status;
            await db.SaveChangesAsync();

            observables.UserEventSignupStream.OnNext(signup);

            await userStatisticsService.UpdateStatisticsAsync(db, callingUserId, callingUser);

            if (EnablePushMessages && pushMessageService != null)
            {
                _ = pushMessageService.PushMessageAsync(
                    "JoinEvent",
                    "A player has joined!",
                    $"{callingUser.DisplayName} signed up for \"{gameEvent.Title}\".", // todo: add a cleverly-formatted date/time?
                    new
                    {
                        type = "JoinEvent",
                        eventId = eventId,
                        userId = callingUserId,
                        eventName = gameEvent.Title,
                        userName = callingUser.DisplayName
                    },
                    eventOwner.DeviceToken
                );
            }

            return signup;
        }

        public async Task<UserEventSignup> LeaveEventAsync(Guid callingUserId, Guid eventId)
        {
            UserEventSignup signup = null;

            var callingUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == callingUserId);
            if (callingUser == null)
            {
                throw new Exception("User not found.");
            }

            if (!await db.Users.AnyAsync(n => n.UserId == callingUserId))
            {
                throw new Exception("User not found.");
            }

            signup = await db.UserEventSignups.FirstOrDefaultAsync(n => n.EventId == eventId && n.UserId == callingUserId);
            if (signup == null)
            {
                throw new Exception("Not signed up to this event.");
            }

            return await LeaveEventAsync(callingUser, signup);
        }

        public async Task<UserEventSignup> LeaveEventAsync(User callingUser, UserEventSignup signup)
        {
            if (callingUser.UserId != signup.UserId)
            {
                throw new Exception("User / UserEventSignup mismatch.");
            }

            // todo: something about forbidding removing it, if the status is about being rejected by the event-owner

            db.UserEventSignups.Remove(signup);
            await db.SaveChangesAsync();

            signup.Status = UserEventStatus.Cancelled;
            observables.UserEventSignupStream.OnNext(signup);

            await userStatisticsService.UpdateStatisticsAsync(db, callingUser.UserId);

            return signup;
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

            bool areFriends = relation.Status == (UserRelationInternalStatus.A_Befriended | UserRelationInternalStatus.B_Befriended);
            if(!areFriends)
            {
                // if the users stopped being friends, then delete signups from each of them for friends-only events created by the other
                async Task removeSignupAsync(User primaryUser, User secondaryUser)
                {
                    var signups = await db.UserEventSignups.Where(n => n.UserId == primaryUser.UserId && n.Event.FriendsOnly && n.Event.CreatedByUserId == secondaryUser.UserId).ToListAsync();
                    if (signups.Any())
                    {
                        foreach(var signup in signups)
                        {
                            await LeaveEventAsync(primaryUser, signup);
                        }
                    }
                }
                await removeSignupAsync(callingUser, friendUser);
                await removeSignupAsync(friendUser, callingUser);
            }

#if EXTRA_EVENTS
            async Task NotifyVisibleEventsAsync(User primaryUser, User secondaryUser, EventAction raisedAction, UserRelation rel)
            {
                // send messages to the primaryUser about friends-only events from the secondaryUser
                var friendsOnlyEvents = await db.Events.Where(n => n.CreatedByUserId == secondaryUser.UserId && n.FriendsOnly).ToListAsync();
                if (friendsOnlyEvents.Any())
                {
                    foreach (var visibleEvent in friendsOnlyEvents)
                    {
                        observables.GameEventStream.OnNext(new EventChangedModel
                        {
                            Event = visibleEvent,
                            ChangingUser = secondaryUser,
                            FriendsOfChangingUser = new[] { rel },
                            Action = raisedAction,
                            RecipientUserId = primaryUser.UserId
                        });
                    }
                }
            }
            if (areFriends)
            {
                await NotifyVisibleEventsAsync(callingUser, friendUser, EventAction.Created, relation);
                await NotifyVisibleEventsAsync(friendUser, callingUser, EventAction.Created, relation);

            }
            else
            {
                await NotifyVisibleEventsAsync(callingUser, friendUser, EventAction.Deleted, relation);
                await NotifyVisibleEventsAsync(friendUser, callingUser, EventAction.Deleted, relation);
            }
#endif

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

        #region Users

        public async Task<User> CreateUserAsync(string displayName, string email, string password, TimeSpan utcOffset, string deviceToken)
        {
            if (!ValidateDisplayName(displayName))
            {
                throw new Exception("Displayname too short.");
            }

            if (!ValidateEmail(email))
            {
                throw new Exception("Email invalid.");
            }

            if (!ValidatePassword(password))
            {
                throw new Exception("Password too weak.");
            }

            var userExists = await db.Users.AnyAsync(n => n.Email == email);
            if (userExists)
            {
                throw new Exception("User with this email already exists.");
            }

            var usedDisplayIds = await db.Users.Where(n => n.DisplayName == displayName).Select(n => n.DisplayId).Distinct().ToListAsync();
            if (usedDisplayIds.Count >= 9999)
            {
                throw new Exception("Too many users with this displayname.");
            }
            var displayId = GetUniqueDisplayId(usedDisplayIds);

            if (utcOffset < -TimeSpan.FromHours(24) || utcOffset > TimeSpan.FromHours(24))
            {
                throw new Exception("UtcOffset larger than 24 hours.");
            }

            var passwordHash = passwordService.CreatePasswordHash(password);

            var newUser = new User
            {
                DisplayName = displayName,
                DisplayId = displayId,
                Email = email,
                PasswordHash = passwordHash,
                UtcOffset = utcOffset,
                DeviceToken = deviceToken
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            // commented out because we currently only send these to friends of the user, and new users by definition have no friends
            /*
            var observableModel = new UserChangedSubscriptionModel
            {
                ChangingUser = newUser,
                FriendsOfChangingUser = new UserRelation[0],
                Action = UserAction.Created
            };
            observables.UserChangeStream.OnNext(observableModel);
            */

            return newUser;
        }

        public async Task<User> ModifyUserAsync(Guid userId, string displayName = null, string email = null, string password = null, string avatar = null, TimeSpan? utcOffset = null, string deviceToken = null)
        {
            var editedUser = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
            if (editedUser == null)
            {
                throw new Exception("User not found.");
            }

            bool wasChangedRelevantForSubscription = false;

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                displayName = displayName.Trim();
                if (!ValidateDisplayName(displayName))
                {
                    throw new Exception("Displayname too short.");
                }
                if (editedUser.DisplayName != displayName)
                {
                    editedUser.DisplayName = displayName;

                    var usedDisplayIds = await db.Users.Where(n => n.DisplayName == displayName).Select(n => n.DisplayId).Distinct().ToListAsync();
                    if (usedDisplayIds.Count >= 9999)
                    {
                        throw new Exception("Too many users with this displayname.");
                    }
                    if (usedDisplayIds.Contains(editedUser.DisplayId))
                    {
                        editedUser.DisplayId = GetUniqueDisplayId(usedDisplayIds);
                    }

                    wasChangedRelevantForSubscription = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                email = email.Trim();
                if (!ValidateEmail(email))
                {
                    throw new Exception("Email invalid.");
                }
                editedUser.Email = email;
            }

            if (!string.IsNullOrEmpty(password))
            {
                if (!ValidatePassword(password))
                {
                    throw new Exception("Password too weak.");
                }
                editedUser.PasswordHash = passwordService.CreatePasswordHash(password);
            }

            if (!string.IsNullOrEmpty(avatar))
            {
                if (!db.Avatars.Any(n => n.ImagePath == avatar))
                {
                    throw new Exception("Avatar not available.");
                }
                else
                {
                    // todo: here we might later add checks around premium-avatars and access rights
                    if (editedUser.AvatarFilename != avatar)
                    {
                        editedUser.AvatarFilename = avatar;
                        wasChangedRelevantForSubscription = true;
                    }
                }
            }

            if (utcOffset.HasValue)
            {
                if (utcOffset < -TimeSpan.FromHours(24) || utcOffset > TimeSpan.FromHours(24))
                {
                    throw new Exception("UtcOffset larger than 24 hours.");
                }
                if (utcOffset != editedUser.UtcOffset)
                {
                    editedUser.UtcOffset = utcOffset;

                    // todo: update to statistics subscription
                }
            }

            if (!string.IsNullOrEmpty(deviceToken))
            {
                editedUser.DeviceToken = deviceToken;
            }

            db.Users.Update(editedUser);
            await db.SaveChangesAsync();

            if (wasChangedRelevantForSubscription)
            {
                var friends = await db.UserRelations
                    .Where(relation => (relation.UserBId == userId || relation.UserAId == userId)) // todo: consider filtering out relations that are not friends or at least are blocked
                    .ToArrayAsync();
                var observableModel = new UserChangedSubscriptionModel
                {
                    ChangingUser = editedUser,
                    FriendsOfChangingUser = friends,
                    Action = UserAction.Updated
                };
                observables.UserChangeStream.OnNext(observableModel);
            }

            return editedUser;
        } 

        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            user.DisplayName = "DELETED_USER";
            user.DisplayId = await db.Users.CountAsync(n => n.SoftDelete) + 2;
            user.AvatarFilename = "avatar_deleted.jpg";
            user.SoftDelete = true;
            user.Email = "DELETED";
            user.PasswordHash = "DELETED";
            db.Users.Update(user);

            // remove signups to events in the future
            var futureSignups = await db.UserEventSignups.Where(n => n.UserId == userId && n.Event.EventDate > DateTime.UtcNow).ToListAsync();
            db.UserEventSignups.RemoveRange(futureSignups);

            foreach (var signup in futureSignups)
            {
                signup.Status = UserEventStatus.Cancelled;
                observables.UserEventSignupStream.OnNext(signup);
            }

            // remove all friends
            var relations = await db.UserRelations.Where(n => n.UserAId == userId || n.UserBId == userId).ToListAsync();
            db.UserRelations.RemoveRange(relations);

            foreach(var relation in relations)
            {
                relation.Status = UserRelationInternalStatus.None;
                observables.UserRelationChangeStream.OnNext(new UserRelationChangedModel
                {
                    ActiveUser = user,
                    ActiveUserAction = UserRelationAction.Remove,
                    Relation = relation,
                    TargetUser = relation.UserAId == userId ? relation.UserB : relation.UserA
                });
            }

            // delete all events that have no signups
            var eventsWithNoSignups = await db.Events.Where(n => n.CreatedByUserId == userId && !n.Signups.Any(nn => nn.UserId != userId)).ToListAsync();
            db.Events.RemoveRange(eventsWithNoSignups);

            foreach(var ev in eventsWithNoSignups)
            {
                observables.GameEventStream.OnNext(new EventChangedModel
                {
                    Action = EventAction.Deleted,
                    ChangingUser = user,
                    Event = ev,
                    FriendsOfChangingUser = new UserRelation[0]
                });
            }

            // delete all friendsonly events that have signups (if they had no signups they would be deleted above)
            var friendsOnlyEventsWithSignups = await db.Events.Where(n => n.CreatedByUserId == userId && n.FriendsOnly && n.Signups.Any(nn => nn.UserId != userId)).Include(n => n.Signups).ToListAsync();
            db.Events.RemoveRange(friendsOnlyEventsWithSignups);

            foreach (var ev in friendsOnlyEventsWithSignups)
            {
                foreach(var signup in ev.Signups.Where(n => n.UserId != userId))
                {
                    await LeaveEventAsync(signup.UserId, signup.EventId);
                }

                observables.GameEventStream.OnNext(new EventChangedModel
                {
                    Action = EventAction.Deleted,
                    ChangingUser = user,
                    Event = ev,
                    FriendsOfChangingUser = new UserRelation[0]
                });
            }

            // observe that the user was deleted
            observables.UserChangeStream.OnNext(new UserChangedSubscriptionModel
            {
                ChangingUser = user,
                FriendsOfChangingUser = relations.ToArray(),
                Action = UserAction.Deleted
            });

            await db.SaveChangesAsync();
        }

        #endregion

        #region Helpers

        // todo: better rules

        static private bool ValidateEmail(string email) => email.Length >= 5 && email.Contains("@");
        static private bool ValidateDisplayName(string displayName) => displayName.Trim().Length >= 3;
        static private bool ValidatePassword(string password) => password.Trim().Length >= 3;

        static private Random rnd = new Random();

        static private int GetUniqueDisplayId(List<int> usedDisplayIds)
        {
            if (usedDisplayIds.Count >= 9999)
                throw new ArgumentException("Too many users with same display name!");

            var range = usedDisplayIds.Count >= 999 ? 9999 : 999;

            while (true)
            {
                // this could obviously be optimized for when the list is long, but it's irrelevant now
                var id = rnd.Next(101, range);
                if (!usedDisplayIds.Contains(id))
                    return id;
            }
        }

        #endregion
    }
}
