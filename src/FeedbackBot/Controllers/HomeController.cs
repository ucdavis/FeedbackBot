using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace FeedbackBot.Controllers
{
    [Authorize(ActiveAuthenticationSchemes = "ucdcas")]
    public class HomeController : Controller
    {
        private readonly AppSettings _appSettings;

        public GitHubClient client;
        public string kerberos;
        public string _appName;

        public HomeController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            client = initialize();
        }

        public string getKerberos()
        {
            return User.Identity.Name;
        }

        public GitHubClient initialize()
        {
            var client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));
            var basicAuth = new Credentials("UCDFeedbackBot", _appSettings.GitHubPW);
            client.Credentials = basicAuth;
            return client;
        }

        public async Task<IActionResult> Index(object sender, EventArgs e)
        {
            var appName = "";
            var queryStrings = Request.Query;
            var keys = queryStrings.Keys;
            var qsList = new Dictionary<string, string>();
            foreach (var key in queryStrings.Keys)
            {
                qsList.Add(key, queryStrings[key]);
            }
            if (qsList.ContainsKey("app"))
            {
                appName = qsList["app"];
                this._appName = appName;
            }
            else
            {
                return RedirectToAction("About");
            }

            // Filters: Created by User, Labels "feedback", and default set to only open feedback issues
            var recently = new IssueRequest
            {
                Filter = IssueFilter.Created,
                Labels = { "feedback" }
            };
            var issues = await client.Issue.GetAllForRepository("ucdavis", _appName);
            // List out all the current issues
            List<issuesContainer> issueContainerList = new List<issuesContainer>();
            foreach (Issue i in issues)
            {
                var newIssueContainer = new issuesContainer();
                newIssueContainer.kerberos = getKerberos();
                newIssueContainer.deserialize(i);
                issueContainerList.Add(newIssueContainer);
            }
            ViewData["AppName"] = appName;
            ViewData["Message"] = "Current Feedback for " + appName;
            return View(issueContainerList);
        }
        
        /*
         * Creates a new issue in FeedbackBot repository
         * @param string title - The title of the feedback [required]
         * @param string descrption - The description of the feedback
         */
        [HttpPost("createIssue")]
        public async Task<ActionResult> CreateIssue(string title, string description, string appName)
        {
            // Initialize new issue
            var createIssue = new NewIssue(title)
            {
                Body = string.Format("{0}\r\n--------------------\r\nVotes: 1\r\nVoters: {1}", description, getKerberos()),
                Labels = { "feedback" }
            };
            var issue = await client.Issue.Create("ucdavis", appName, createIssue);
            return RedirectToAction("index", "home", new { app = appName });
        }

        [HttpPost("addComment")]
        public async Task<ActionResult> AddComment(string comment, string voteID, string appName)
        {
            int issueIDInt = Int32.Parse(voteID);
            comment = string.Format("{0} \r\n--------------------\r\nAuthor: {1}", comment, getKerberos());
            var issue = await client.Issue.Get("ucdavis", appName, issueIDInt);
            var addComment = await client.Issue.Comment.Create("ucdavis", appName, issueIDInt, comment);
            return RedirectToAction("details", "home", new { appName = appName, id = voteID });
        }

        /*
         * Upvotes a feedback issue
         * @param string voteID - The number of the issue we are upvoting
         */
        [HttpPost("vote")]
        public async Task<ActionResult> Vote(string voteID, string appName)
        {
            int issueIDInt = Int32.Parse(voteID);

            var issue = await client.Issue.Get("ucdavis", appName, issueIDInt);
            var newIssueContainer = new issuesContainer();
            newIssueContainer.kerberos = getKerberos();
            newIssueContainer.deserialize(issue);

            if (newIssueContainer.voteState == "unvote")
            {
                int newVoteCount = newIssueContainer.numOfVotesInt - 1;
                newIssueContainer.numOfVotesInt = newVoteCount;
                newIssueContainer.numOfVotes = newVoteCount.ToString();
                newIssueContainer.listOfVoters.Remove(getKerberos());
            }
            else
            {
                // Update vote count
                int newVoteCount = newIssueContainer.numOfVotesInt + 1;
                newIssueContainer.numOfVotesInt = newVoteCount;
                newIssueContainer.numOfVotes = newVoteCount.ToString();
                newIssueContainer.listOfVoters.Add(getKerberos());
            }
            newIssueContainer.stringOfVoters = string.Join(",", newIssueContainer.listOfVoters.ToArray()).Trim().TrimStart(',');

            // Update issue on GitHub
            var update = issue.ToUpdate();
            update.Body = newIssueContainer.serialize();
            await client.Issue.Update("ucdavis", appName, issueIDInt, update);

            //Update voters
            return RedirectToAction("details", "home", new { appName = appName, id = voteID });
        }

        /*
         * Displays the details of an issue along with all its comments
         * @param string voteID - The number of the issue we are upvoting
         */
        [HttpGet("/issues/{appName}/{id}")]
        public async Task<IActionResult> Details(string appName, string id)
        {
            int issueIDInt = Int32.Parse(id);

            // Getting issue
            var issue = await client.Issue.Get("ucdavis", appName, issueIDInt);
            var newIssueContainer = new issuesContainer();
            newIssueContainer.kerberos = getKerberos();
            newIssueContainer.deserialize(issue);

            // Getting all comments in the issue
            var issueComments = await client.Issue.Comment.GetAllForIssue("ucdavis", appName, issueIDInt);
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

            ViewData["AppName"] = appName;
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
                var returnBody = string.Format("{0}\r\n--------------------\r\nVotes: {1}\r\nVoters: {2}", this.body, numOfVotes, stringOfVoters);
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
                this.stringOfVoters = issueBody.Substring(indexOfVoters + 7);
                this.listOfVoters = this.stringOfVoters.Split(',').Select(d => d.Trim()).ToList();

                if (this.stringOfVoters.IndexOf(this.kerberos) > 0)
                {
                    this.voteState = "unvote";
                }
                else
                {
                    this.voteState = "vote";
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
