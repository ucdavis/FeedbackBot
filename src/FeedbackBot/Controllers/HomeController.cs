using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Text.RegularExpressions;

namespace FeedbackBot.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            client.Credentials = basicAuth;
            var user = await client.User.Current();
            var recently = new IssueRequest
            {
                Filter = IssueFilter.Created,
                Labels = { "feedback" }
            };
            var issues = await client.Issue.GetAllForCurrent(recently);
            List<issuesContainer> issueContainerList = new List<issuesContainer>();
            foreach (Issue i in issues)
            {
                var newIssueContainer = new issuesContainer();

                var issueBody = i.Body;
                var stringPattern = "(?<=Votes: )[0-9]+";
                Match m = Regex.Match(issueBody, stringPattern);
                var indexOfLine = issueBody.IndexOf("--------------------");
                var indexOfVoters = issueBody.IndexOf("Voters:");
                newIssueContainer.title = i.Title;
                newIssueContainer.numOfVotes = m.Value;
                newIssueContainer.listOfVoters = issueBody.Substring(indexOfVoters+7);
                newIssueContainer.body = issueBody.Substring(0, indexOfLine);
                newIssueContainer.number = i.Number;
                issueContainerList.Add(newIssueContainer);
            }
            
            ViewData["Message"] = "Current Feedback";
            return View(issueContainerList);
        }
        
        [HttpPost("createIssue")]
        public async Task<ActionResult> CreateIssue(string title, string description)
        {
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            client.Credentials = basicAuth;
            var user = await client.User.Current();
            var createIssue = new NewIssue(title)
            {
                Body = description + "\r\n--------------------\r\nVotes: 0\r\nVoters: ",
                Labels = { "feedback" }
            };
            var issue = await client.Issue.Create("ucdavis", "FeedbackBot", createIssue);
            return RedirectToAction("index");
        }


        [HttpPost("vote")]
        public async Task<ActionResult> Vote(string voteID)
        {
            int issueIDInt = Int32.Parse(voteID);
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            var basicAuth = new Credentials("UCDFeedbackBot", "99LinesOfCode"); // NOTE: not real credentials
            client.Credentials = basicAuth;
            var user = await client.User.Current();
            var issue = await client.Issue.Get("ucdavis", "FeedbackBot", issueIDInt);
            var stringPattern = "(?<=Votes: )[0-9]+";
            Match m = Regex.Match(issue.Body, stringPattern);   
            int newVoteCount = int.Parse(m.Value) + 1;
            var update = issue.ToUpdate();

            return RedirectToAction("index");
        }

        public class issuesContainer {
            public string title { get; set; }
            public string numOfVotes { get; set; }
            public string listOfVoters { get; set; }
            public string body { get; set; }
            public int number { get; set; }
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
