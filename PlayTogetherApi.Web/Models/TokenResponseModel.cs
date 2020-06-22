using System;

namespace PlayTogetherApi.Web.Models
{
    public class TokenResponseModel
    {
        public string Token_type { get; set; } = "bearer";
        public string Access_token { get; set; }
        public string Refresh_token { get; set; }
        public int Expires_in { get; set; } = 90 * 60;
    }
}
