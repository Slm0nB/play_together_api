using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Data;

namespace PlayTogetherApi.Services
{
    public class AuthenticationService
    {
        readonly IConfiguration _config;
        readonly Data.PlayTogetherDbContext _dbContext;

        public AuthenticationService(IConfiguration config, PlayTogetherDbContext dbContext)
        {
            _config = config;
            _dbContext = dbContext;
        }

        public async Task<TokenResponseModel> RequestTokenAsync(TokenRequestModel dto)
        {
            switch (dto?.Grant_type?.ToLowerInvariant())
            {
                case "password":
                case "username_password":
                    {
                        var passwordHash = CreatePasswordHash(dto.Password);
                        var user = await _dbContext.Users.FirstOrDefaultAsync(n => n.Email == dto.Username && n.PasswordHash == passwordHash);
                        if (user != null)
                        {
                            var refreshToken = await CreateRefreshTokenForUserAsync(user.UserId);
                            var refreshTokenString = refreshToken.Token.ToString("N");
                            var accessTokenString = BuildJwt(user);

                            return new TokenResponseModel
                            {
                                Access_token = accessTokenString,
                                Refresh_token = refreshTokenString
                            };
                        }
                    }
                    break;

                case "refresh_token":
                    {
                        if (dto.Refresh_token.HasValue)
                        {
                            var refreshToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(n => n.Token == dto.Refresh_token.Value);
                            if (refreshToken != null)
                            {
                                var user = await _dbContext.Users.FirstOrDefaultAsync(n => n.UserId == refreshToken.UserId);
                                if (user != null)
                                {
                                    var accessTokenString = BuildJwt(user);

                                    return new TokenResponseModel
                                    {
                                        Access_token = accessTokenString
                                    };
                                }
                            }
                        }
                    }
                    break;

                case "api_key":
                    throw new NotImplementedException();
            }

            return null;
        }

        public async Task<Data.RefreshToken> CreateRefreshTokenForUserAsync(Guid userId, TimeSpan? lifetime = null)
        {
            var refreshToken = new Data.RefreshToken
            {
                Token = Guid.NewGuid(),
                UserId = userId,
                CreatedDate = DateTime.Now,
                ExpirationDate = DateTime.Now + (lifetime ?? TimeSpan.FromDays(30))
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return refreshToken;
        }

        #region Helpers

        public string CreatePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "secretsaltbutnopepper"));
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                return hash;
            }
        }

        #endregion

        #region JWT

        public string BuildJwt(Data.User user)
        {
            return BuildJwt(
                new[] {
                    new Claim("userid", user.UserId.ToString()),
                  },
                TimeSpan.FromMinutes(90));
        }

        public string BuildJwt(Claim[] claims, TimeSpan expiration)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
              _config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              claims: claims.Where(n => n != null).ToArray(),
              expires: DateTime.Now + expiration,
              signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public JwtSecurityToken ValidateJwt(string accessToken)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));

            var validationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Issuer"]
            };

            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(accessToken, validationParameters, out var validToken);
            var validJwt = validToken as JwtSecurityToken;

            if (validJwt == null)
            {
                throw new ArgumentException("Invalid JWT");
            }

            if (!validJwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                throw new ArgumentException("Algorithm must be HmacSha256");
            }

            return validJwt;
        }

        #endregion
    }
}
