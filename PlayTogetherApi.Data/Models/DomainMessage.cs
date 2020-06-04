using System;
using System.ComponentModel.DataAnnotations;

namespace PlayTogetherApi.Data.Models
{
    public enum DomainMessageType
    {
        None = 0,
        NewUserSignup = 1,
        UserUpdated = 2,
        UserDeleted = 3,
        EventCreated = 4,
        EventUpdated = 5,
        EventDeleted = 6,
        EventSignupChanged = 7,
        UserRelationChanged = 8,

    }


    public class DomainMessage
    {
        [Key]
        public long Id { get; set; }
        public DomainMessageType MessageType { get; set; }
        public DateTime Time { get; set; }


        public Guid UserId { get; set; }
        public Guid User2Id { get; set; }
        public Guid GameId { get; set; }
        public Guid EventId { get; set; }
        public Guid UserEventSignupId { get; set; }
        public Guid UserRelationId { get; set; }

        public string JsonPayload;
    }
}
