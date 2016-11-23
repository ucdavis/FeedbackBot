using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;

namespace FeedbackBot.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            var basicAuth = new Credentials("", ""); // NOTE: not real credentials
            client.Credentials = basicAuth;
            var user = await client.User.Current();
            var recently = new IssueRequest
            {
                Filter = IssueFilter.Created,
                Since = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(14))
            };
            var issues = await client.Issue.GetAllForCurrent(recently);

            ViewData["Message"] = "Current Feedback";
            return View(issues);
        }


        public IActionResult About()
        {
            ViewData["Message"] = "Your application descriptieeeeeeon page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
