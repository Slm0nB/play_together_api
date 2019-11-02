using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PlayTogetherApi.Domain
{
    public class PlayTogetherDbContext : DbContext
    {
        public DbSet<Event> Events { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<BuiltinAvatar> Avatars { get; set; }
        public DbSet<UserEventSignup> UserEventSignups { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

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
