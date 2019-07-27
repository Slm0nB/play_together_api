using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GameCalendarApi.Domain
{
    public class SecurityService
    {
        readonly IConfiguration _config;

        public SecurityService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePasswordHash(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "secretsaltbutnopepper"));
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                return hash;
            }
        }

        public string BuildJwt(User user)
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
    }
}
