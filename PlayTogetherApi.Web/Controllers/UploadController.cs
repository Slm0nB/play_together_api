using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayTogetherApi.Web.Models;
using PlayTogetherApi.Data;
using PlayTogetherApi.Services;
using Microsoft.AspNetCore.Http;

namespace PlayTogetherApi.Web.Controllers
{
    [ApiController]
    public class UploadController : ControllerBase
    {
        S3Service s3service;
        PlayTogetherDbContext db;

        public UploadController(S3Service s3service, PlayTogetherDbContext db)
        {
            this.s3service = s3service;
            this.db = db;
        }

        [Authorize]
        [Route("api/upload-avatar")]
        [HttpPost]
        public async Task<IActionResult> UploadAvatar(List<IFormFile> files)
        {
            if (!Guid.TryParse(User.Claims.FirstOrDefault(n => n.Type == "userid")?.Value, out var userid))
                throw new UnauthorizedAccessException();

            var alternateFiles = this.Request.Form.Files;
            var file = files?.FirstOrDefault() ?? alternateFiles?.FirstOrDefault();
            if (file == null)
                throw new Exception(); // todo: return "bad request" instead

            var user = await db.Users.FirstOrDefaultAsync(n => n.UserId == userid);
            if(user == null)
                throw new Exception(); // todo: return "bad request" instead

            var filename = "avatar-" + userid.ToString("N") + System.IO.Path.GetExtension(file.FileName);

            using (var stream = file.OpenReadStream())
            {
                await s3service.UploadFileAsync(stream, filename);
            }

            if (user.AvatarFilename != filename)
            {
                user.AvatarFilename = filename;
                db.Users.Update(user);
                await db.SaveChangesAsync();
            }

            return Ok(new
            {
                hello = "world",
                claims = this.HttpContext.User.Claims
            });
        }
    }
}
