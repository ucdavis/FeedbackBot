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

        public static class Globals
        {
            public static String kerberos { get; set; } // Modifiable in Code
        }

        public GitHubClient initialize()
        {
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            var basicAuth = new Credentials("UCDFeedbackBot", "99LinesOfCode");
            client.Credentials = basicAuth;
            return client;
        }

        public async Task<IActionResult> Index()
        {
            // Github Authentication
            var client = initialize();
            Globals.kerberos = User.Identity.Name;

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
                newIssueContainer.deserialize(i, Globals.kerberos);
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
            var client = initialize();

            // Initialize new issue
            var createIssue = new NewIssue(title)
            {
                Body = description + "\r\n--------------------\r\nVotes: 1\r\nVoters: " + Globals.kerberos,
                Labels = { "feedback" }
            };
            var issue = await client.Issue.Create("ucdavis", "FeedbackBot", createIssue);
            return RedirectToAction("index");
        }

        [HttpPost("addComment")]
        public async Task<ActionResult> AddComment(string comment, string voteID)
        {
            int issueIDInt = Int32.Parse(voteID);

            // Github Authentication
            var client = initialize();

            comment = comment + "\r\n--------------------\r\nAuthor: " + Globals.kerberos;
            var issue = await client.Issue.Get("ucdavis", "FeedbackBot", issueIDInt);
            var addComment = await client.Issue.Comment.Create("ucdavis", "FeedbackBot", issueIDInt, comment);
            return Redirect("/issues/" + voteID);
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
            var client = initialize();

            var issue = await client.Issue.Get("ucdavis", "FeedbackBot", issueIDInt);
            var newIssueContainer = new issuesContainer();
            newIssueContainer.deserialize(issue, Globals.kerberos);

            if (newIssueContainer.voteState == "unvote")
            {
                int newVoteCount = newIssueContainer.numOfVotesInt - 1;
                newIssueContainer.numOfVotesInt = newVoteCount;
                newIssueContainer.numOfVotes = newVoteCount.ToString();

                newIssueContainer.listOfVoters.Remove(Globals.kerberos);
            }
            else
            {
                // Update vote count
                int newVoteCount = newIssueContainer.numOfVotesInt + 1;
                newIssueContainer.numOfVotesInt = newVoteCount;
                newIssueContainer.numOfVotes = newVoteCount.ToString();
                newIssueContainer.listOfVoters.Add(Globals.kerberos);
            }
            newIssueContainer.stringOfVoters = string.Join(",", newIssueContainer.listOfVoters.ToArray()).Trim().TrimStart(',');

            // Update issue on GitHub
            var update = issue.ToUpdate();
            update.Body = newIssueContainer.serialize();
            await client.Issue.Update("ucdavis", "FeedbackBot", issueIDInt, update);

            //Update voters
            return RedirectToAction("index");
        }

        [HttpGet("/issues/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            int issueIDInt = Int32.Parse(id);

            // Github Authentication
            var client = initialize();

            // Getting issue
            var issue = await client.Issue.Get("ucdavis", "FeedbackBot", issueIDInt);
            var newIssueContainer = new issuesContainer();
            newIssueContainer.deserialize(issue, Globals.kerberos);

            // Getting all comments in the issue
            var issueComments = await client.Issue.Comment.GetAllForIssue("ucdavis", "FeedbackBot", issueIDInt);
            var listOfComments = new List<commentContainer>();
            foreach (IssueComment i in issueComments)
            {
                var commentContainer = new commentContainer();
                commentContainer.deserialize(i);
                listOfComments.Add(commentContainer);
            }

            // Update model view
            var issuesView = new issueDetailsViewModel();
            issuesView.comments = listOfComments;
            issuesView.issue = newIssueContainer;

            return View(issuesView);
        }

        public class issueDetailsViewModel
        {
            public issuesContainer issue { get; set; }
            public List<commentContainer> comments { get; set; }
        }

        public class commentContainer
        {
            public string body { get; set; }
            public string author { get; set; }
            public string createDate { get; set; }

            public void deserialize(Octokit.IssueComment comment)
            {
                var indexOfLine = comment.Body.IndexOf("--------------------");
                this.body = comment.Body.Substring(0, indexOfLine);
                var indexOfAuthor = comment.Body.IndexOf("Author:");
                this.author = comment.Body.Substring(indexOfAuthor + 7);
                this.createDate = comment.CreatedAt.ToString().Remove(9);
            }
        }

        public class issuesContainer {
            public string title { get; set; }
            public string numOfVotes { get; set; }
            public int numOfVotesInt { get; set; }
            public string stringOfVoters { get; set; }
            public List<string> listOfVoters { get; set; }
            public string body { get; set; }
            public int number { get; set; }
            public string voteState { get; set; }
            public string kerberos { get; set; }

            // Returns string of description for GitHub issue body
            public string serialize()
            {
                var returnBody = this.body + "\r\n--------------------\r\nVotes: " + numOfVotes + "\r\nVoters: " + stringOfVoters;
                return returnBody;
            }

            public void deserialize(Octokit.Issue issue, string kerberos)
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
                this.stringOfVoters = issueBody.Substring(indexOfVoters + 7);
                this.listOfVoters = this.stringOfVoters.Split(',').Select(d => d.Trim()).ToList();

                if (issueBody.IndexOf(kerberos) > 0)
                {
                    voteState = "unvote";
                } else
                {
                    voteState = "vote";
                }
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
