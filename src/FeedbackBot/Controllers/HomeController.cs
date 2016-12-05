using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace FeedbackBot.Controllers
{
    [Authorize(ActiveAuthenticationSchemes = "ucdcas")]
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index()
        {
            // Github Authentication
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            var basicAuth = new Credentials("UCDFeedbackBot", ""); 
            client.Credentials = basicAuth;
            var user = await client.User.Current();

            // Filters: Created by User, Labels "feedback", and default set to only open feedback issues
            var recently = new IssueRequest
            {
                Filter = IssueFilter.Created,
                Labels = { "feedback" }
            };
            var issues = await client.Issue.GetAllForCurrent(recently);

            // List out all the current issues
            List<issuesContainer> issueContainerList = new List<issuesContainer>();
            foreach (Issue i in issues)
            {
                var newIssueContainer = new issuesContainer();
                newIssueContainer.deserialize(i);
                issueContainerList.Add(newIssueContainer);
            }
            ViewData["Message"] = "Current Feedback";
            return View(issueContainerList);
        }
        
        /*
         * Creates a new issue in FeedbackBot repository
         * @param string title - The title of the feedback [required]
         * @param string descrption - The description of the feedback
         */
        [HttpPost("createIssue")]
        public async Task<ActionResult> CreateIssue(string title, string description)
        {
            // Github Authentication
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            var basicAuth = new Credentials("UCDFeedbackBot", ""); 
            client.Credentials = basicAuth;
            var user = await client.User.Current();

            // Initialize new issue
            var createIssue = new NewIssue(title)
            {
                Body = description + "\r\n--------------------\r\nVotes: 0\r\nVoters: ",
                Labels = { "feedback" }
            };
            var issue = await client.Issue.Create("ucdavis", "FeedbackBot", createIssue);
            return RedirectToAction("index");
        }

        /*
         * Upvotes a feedback issue
         * @param string voteID - The number of the issue we are upvoting
         */
        [HttpPost("vote")]
        public async Task<ActionResult> Vote(string voteID)
        {
            int issueIDInt = Int32.Parse(voteID);

            // Github Authentication
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            var basicAuth = new Credentials("UCDFeedbackBot", ""); // NOTE: not real credentials
            client.Credentials = basicAuth;
            var user = await client.User.Current();

            var issue = await client.Issue.Get("ucdavis", "FeedbackBot", issueIDInt);
            var newIssueContainer = new issuesContainer();
            newIssueContainer.deserialize(issue);

            // Update vote count
            int newVoteCount = newIssueContainer.numOfVotesInt + 1;
            newIssueContainer.numOfVotesInt = newVoteCount;
            newIssueContainer.numOfVotes = newVoteCount.ToString();
            
            // Update issue on GitHub
            var update = issue.ToUpdate();
            update.Body = newIssueContainer.serialize();
            await client.Issue.Update("ucdavis", "FeedbackBot", issueIDInt, update);

            return RedirectToAction("index");
        }

        public class issuesContainer {
            public string title { get; set; }
            public string numOfVotes { get; set; }
            public int numOfVotesInt { get; set; }
            public string listOfVoters { get; set; }
            public string[] arrayOfVoters { get; set; }
            public string body { get; set; }
            public int number { get; set; }

            // Returns string of description for GitHub issue body
            public string serialize()
            {
                var returnBody = this.body + "\r\n--------------------\r\nVotes: " + numOfVotes + "\r\nVoters: " + listOfVoters;
                return returnBody;
            }

            public void deserialize(Octokit.Issue issue)
            {
                // Issue body, title, and issue number
                var issueBody = issue.Body;
                this.title = issue.Title;
                this.number = issue.Number;

                // Votes
                var stringPattern = "(?<=Votes: )[0-9]+";
                Match m = Regex.Match(issueBody, stringPattern);
                this.numOfVotes = m.Value;
                this.numOfVotesInt = int.Parse(numOfVotes);

                // Feedback
                var indexOfLine = issueBody.IndexOf("--------------------");
                this.body = issueBody.Substring(0, indexOfLine);

                // Voters
                var indexOfVoters = issueBody.IndexOf("Voters:");
                Char delimiter = ',';
                this.listOfVoters = issueBody.Substring(indexOfVoters + 7);
                this.arrayOfVoters = this.listOfVoters.Split(delimiter);
            }
        }


        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

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
