using System.Threading.Tasks;
using AspNetCore.Security.CAS;
using FeedbackBot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FeedbackBot.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthSettings _authSettings;
        public AccountController(IOptions<AuthSettings> authSettings)
        {
            _authSettings = authSettings.Value;
        }
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        [Route("login")]
        public async Task Login(string returnUrl)
        {
            var props = new AuthenticationProperties { RedirectUri = returnUrl };
            await HttpContext.ChallengeAsync(CasDefaults.AuthenticationScheme, props);
        }

        [Route("logout")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Redirect($"{_authSettings.CasBaseUrl}logout");
        }

    }
}
