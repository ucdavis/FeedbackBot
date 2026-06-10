using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using FeedbackBot.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace FeedbackBot.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthSettings _authSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IOptions<AuthSettings> authSettings, IHttpClientFactory httpClientFactory)
        {
            _authSettings = authSettings.Value;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        [Route("login")]
        public async Task<IActionResult> Login(string returnUrl = "/", string ticket = null)
        {
            if (_authSettings.UseLocalAuth)
            {
                await SignInUser("local-dev");
                return LocalRedirect(GetSafeReturnUrl(returnUrl));
            }

            var serviceUrl = BuildServiceUrl(returnUrl);
            if (string.IsNullOrWhiteSpace(ticket))
            {
                return Redirect(BuildCasUrl("login", new Dictionary<string, string>
                {
                    ["service"] = serviceUrl
                }));
            }

            var userName = await ValidateTicket(serviceUrl, ticket);
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Unauthorized();
            }

            await SignInUser(userName);
            return LocalRedirect(GetSafeReturnUrl(returnUrl));
        }

        [Route("logout")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (_authSettings.UseLocalAuth)
            {
                return RedirectToAction("Index", "Home");
            }

            return Redirect($"{_authSettings.CasBaseUrl}logout");
        }

        private async Task SignInUser(string userName)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.NameIdentifier, userName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }

        private string BuildServiceUrl(string returnUrl)
        {
            return QueryHelpers.AddQueryString(
                $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}",
                "returnUrl",
                GetSafeReturnUrl(returnUrl));
        }

        private string BuildCasUrl(string path, IDictionary<string, string> query)
        {
            var baseUri = new UriWithTrailingSlash(_authSettings.CasBaseUrl);
            var url = new System.Uri(baseUri.Value, path).ToString();
            return QueryHelpers.AddQueryString(url, query);
        }

        private async Task<string> ValidateTicket(string serviceUrl, string ticket)
        {
            var validationUrl = BuildCasUrl("serviceValidate", new Dictionary<string, string>
            {
                ["service"] = serviceUrl,
                ["ticket"] = ticket
            });

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync(validationUrl);
            var document = XDocument.Parse(response);
            var userElement = document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "user");

            return userElement?.Value;
        }

        private string GetSafeReturnUrl(string returnUrl)
        {
            return Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        }

        private class UriWithTrailingSlash
        {
            public UriWithTrailingSlash(string value)
            {
                Value = new System.Uri(value.EndsWith("/") ? value : $"{value}/");
            }

            public System.Uri Value { get; }
        }
    }
}
