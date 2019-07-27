using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace GameCalendarApi.Domain
{
    public class GameCalendarDbContext : DbContext
    {
        private readonly SecurityService _securityService;

        public DbSet<Event> Events { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public GameCalendarDbContext(SecurityService securityService)
        {
            _securityService = securityService;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("GameCalendarConnectionString"));
        }

        public async Task<User> AuthenticateUserAsync(string email, string password)
        {
            var passwordHash = _securityService.CreatePasswordHash(password);
            var user = await Users.FirstOrDefaultAsync(n => n.Email == email && n.PasswordHash == passwordHash);
            return user;
        }

        public async Task<RefreshToken> CreateRefreshTokenForUserAsync(Guid userId, TimeSpan? lifetime = null)
        {
            var refreshToken = new RefreshToken {
                Token = Guid.NewGuid(),
                UserId = userId,
                CreatedDate = DateTime.Now,
                ExpirationDate = DateTime.Now + (lifetime ?? TimeSpan.FromDays(30))
            };

            RefreshTokens.Add(refreshToken);
            await SaveChangesAsync();

            return refreshToken;
        }
    }
}
