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
using System.Collections.ObjectModel;

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
            var basicAuth = new Credentials(_appSettings.GitHubUser, _appSettings.GitHubPassword);
            client.Credentials = basicAuth;
            return client;
        }

        public IActionResult Index()
        {
            ViewData["Message"] = "Welcome";
            return View();
        }

        [HttpGet("/app/{appName}")]
        public async Task<IActionResult> app(string appName)
        {
            // Filters: Created by User, Labels "feedback", and default set to only open feedback issues
            var recently = new RepositoryIssueRequest
            {
                Filter = IssueFilter.Created,
                Labels = { "feedback" }
            };
            var issues = await client.Issue.GetAllForRepository("ucdavis", appName, recently);

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
                Body = string.Format("{0}\r\n--------------------\r\nVotes: 1\r\nAuthor: {1}\r\nVoters: {2}", description, getKerberos(), getKerberos()),
                Labels = { "feedback" }
            };
            var issue = await client.Issue.Create("ucdavis", appName, createIssue);
            return RedirectToAction("app", "home", new { appName });
        }

        [HttpPost("search")]
        public async Task<ActionResult> Search(string searchInput, string appName)
        {
            if (searchInput == null)
            {
                return RedirectToAction("app", "home", new { appName });
            }
            var request = new SearchIssuesRequest(searchInput);
            request.Repos.Add("ucdavis", appName);
            request.Labels = new List<string>() { "feedback" }; ;
            request.State = ItemState.Open;
            var repos = await client.Search.SearchIssues(request);
            List<issuesContainer> issueContainerList = new List<issuesContainer>();
            foreach (Issue i in repos.Items)
            {
                var newIssueContainer = new issuesContainer();
                newIssueContainer.kerberos = getKerberos();
                newIssueContainer.deserialize(i);
                issueContainerList.Add(newIssueContainer);
            }
            ViewData["AppName"] = appName;
            ViewData["SearchTerm"] = searchInput;
            ViewData["Message"] = "Search Results For  " + searchInput + " In " + appName;
            return View(issueContainerList);
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

            TempData["voteValidationMessage"] = "Thank you for voting on this issue!";  
            // Update voters
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
            if (TempData["voteValidationMessage"] != null)
            {
                issuesView.voteMessage = TempData["voteValidationMessage"].ToString();
            }
            ViewData["AppName"] = appName;
            return View(issuesView);
        }


            {
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
