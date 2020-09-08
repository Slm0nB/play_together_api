using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
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
        readonly PasswordService _passwordService;

        public AuthenticationService(IConfiguration config, PlayTogetherDbContext dbContext, PasswordService passwordService)
        {
            _config = config;
            _dbContext = dbContext;
            _passwordService = passwordService;
        }

        public async Task<TokenResponseModel> RequestTokenAsync(TokenRequestModel dto)
        {
            var defaultAccessTokenLifetime = 90;
            var defaultRefreshTokenLifetime = 60 * 24 * 30;

            dto.AccessTokenLifetime = Math.Min(defaultAccessTokenLifetime, Math.Max(2, dto.AccessTokenLifetime ?? defaultAccessTokenLifetime));
            dto.RefreshTokenLifetime = Math.Min(defaultRefreshTokenLifetime, Math.Max(2, dto.RefreshTokenLifetime ?? defaultRefreshTokenLifetime));

            var accessTokenLifetime = TimeSpan.FromMinutes(dto.AccessTokenLifetime.Value);
            var refreshTokenLifetime = TimeSpan.FromMinutes(dto.RefreshTokenLifetime.Value);

            switch (dto?.Grant_type?.ToLowerInvariant())
            {
                case "password":
                case "username_password":
                    {
                        var passwordHash = _passwordService.CreatePasswordHash(dto.Password);
                        var user = await _dbContext.Users.FirstOrDefaultAsync(n => n.Email == dto.Username && n.PasswordHash == passwordHash);
                        if (user != null)
                        {
                            var refreshToken = await CreateRefreshTokenForUserAsync(user.UserId, refreshTokenLifetime);
                            var refreshTokenString = refreshToken.Token.ToString("N");
                            var accessTokenString = BuildJwt(user, accessTokenLifetime);

                            return new TokenResponseModel
                            {
                                Access_token = accessTokenString,
                                Refresh_token = refreshTokenString,
                                Expires_in = Convert.ToInt32(accessTokenLifetime.TotalMinutes)
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
                                    var accessTokenString = BuildJwt(user, accessTokenLifetime);

                                    return new TokenResponseModel
                                    {
                                        Access_token = accessTokenString,
                                        Expires_in = Convert.ToInt32(accessTokenLifetime.TotalMinutes)
                                        // todo: add a new refresh token if the old one is close to expiry
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

        public async Task<Data.RefreshToken> CreateRefreshTokenForUserAsync(Guid userId, TimeSpan lifetime)
        {
            var refreshToken = new Data.RefreshToken
            {
                Token = Guid.NewGuid(),
                UserId = userId,
                CreatedDate = DateTime.Now,
                ExpirationDate = DateTime.Now + lifetime
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return refreshToken;
        }

        #region JWT

        public string BuildJwt(Data.User user, TimeSpan lifetime)
        {
            return BuildJwt(
                new[] {
                    new Claim("userid", user.UserId.ToString()),
                },
                lifetime);
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
