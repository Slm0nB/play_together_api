using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameCalendarApi.Web.Models;
using GameCalendarApi.Domain;

namespace GameCalendarApi.Web.Controllers
{
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly GameCalendarDbContext _dbContext;
        private readonly SecurityService _securityService;

        public AuthenticationController(GameCalendarDbContext dbContext, SecurityService securityService)
        {
            _dbContext = dbContext;
            _securityService = securityService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("api/token")]
        public async Task<IActionResult> GetAccessToken([FromForm]TokenRequestModel dto)
        {
            IActionResult response = Unauthorized();

            switch (dto?.Grant_type?.ToLowerInvariant())
            {
                case "password":
                case "username_password":
                    {
                        var user = await _dbContext.AuthenticateUserAsync(dto.Username, dto.Password);
                        if (user != null)
                        {
                            var refreshToken = await _dbContext.CreateRefreshTokenForUserAsync(user.UserId);
                            var refreshTokenString = refreshToken.Token.ToString("N");
                            var accessTokenString = _securityService.BuildJwt(user);

                            response = Ok(new TokenResponseModel
                            {
                                Access_token = accessTokenString,
                                Refresh_token = refreshTokenString
                            });
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
                                    var accessTokenString = _securityService.BuildJwt(user);
                                    response = Ok(new TokenResponseModel
                                    {
                                        Access_token = accessTokenString
                                    });
                                }
                            }
                        }
                    }
                    break;

                case "api_key":
                    throw new NotImplementedException();
            }

            return response;
        }
    }
}
