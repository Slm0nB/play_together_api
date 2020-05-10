using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PlayTogetherApi.Data
{
    public class PlayTogetherDbContext : DbContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserRelation> UserRelations { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<BuiltinAvatar> Avatars { get; set; }
        public DbSet<UserEventSignup> UserEventSignups { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PlaceholderImage> PlaceholderImages { get; set; }

        public PlayTogetherDbContext()
        {

        }

        public PlayTogetherDbContext(DbContextOptions<PlayTogetherDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEventSignup>()
                .HasKey(t => new { t.UserId, t.EventId });

            modelBuilder.Entity<UserRelation>()
                .HasKey(t => new { t.UserAId, t.UserBId });

            modelBuilder.Entity<User>()
                .HasIndex(t => new { t.DisplayName, t.DisplayId })
                .IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("PlayTogetherConnectionString"));
            }
        }
    }
}
