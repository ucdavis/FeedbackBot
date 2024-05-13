using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using FeedbackBot.Models;
using FeedbackBot.Services;
using Microsoft.AspNetCore.Authorization;
using Octokit;

namespace FeedbackBot.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IGitHubService _gitHubService;

        public HomeController(IGitHubService gitHubService)
        {
            _gitHubService = gitHubService;
        }

        public IActionResult Index()
        {
            ViewData["Message"] = "Welcome";

            return View();
        }

        [HttpGet("/app/{appName}")]
        public async Task<IActionResult> App(string appName)
        {
            var issues = await _gitHubService.GetIssues(appName); 

            // List out all the current issues
            var issueContainerList = new List<IssueContainer>();
            foreach (var i in issues)
            {
                var newIssueContainer = new IssueContainer { Kerberos = User.Identity.Name };
                newIssueContainer.Deserialize(i);
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
                Body =
                    $"{description}\r\n" +
                    "--------------------\r\n" +
                    "Votes: 1\r\n" +
                    $"Author: {User.Identity.Name}\r\n" +
                    $"Voters: {User.Identity.Name}",
                Labels = { "feedback" }
            };
            var issue = await _gitHubService.CreateIssue(appName, createIssue);
            return RedirectToAction("App", "home", new { appName });
        }

        [HttpPost("search")]
        public async Task<ActionResult> Search(string searchInput, string appName)
        {
            if (searchInput == null)
            {
                return RedirectToAction("App", "home", new { appName });
            }

            var results = await _gitHubService.Search(appName, searchInput);

            var issueContainerList = new List<IssueContainer>();
            foreach (var i in results.Items)
            {
                var newIssueContainer = new IssueContainer { Kerberos = User.Identity.Name };
                newIssueContainer.Deserialize(i);
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
            var id = int.Parse(voteID);
            var body = $"{comment} \r\n" +
                      "--------------------\r\n" +
                      $"Author: {User.Identity.Name}";
            var issue = await _gitHubService.GetIssue(appName, id);
            var result = await _gitHubService.AddComment(appName, id, body);

            return RedirectToAction("details", "home", new { appName = appName, id = voteID });
        }

        /*
         * Upvotes a feedback issue
         * @param string voteID - The number of the issue we are upvoting
         */
        [HttpPost("vote")]
        public async Task<ActionResult> Vote(string voteID, string appName)
        {
            var id = int.Parse(voteID);

            var issue = await _gitHubService.GetIssue(appName, id);
            var newIssueContainer = new IssueContainer { Kerberos = User.Identity.Name };

            newIssueContainer.Deserialize(issue);

            if (newIssueContainer.VoteState == "unvote")
            {
                var newVoteCount = newIssueContainer.NumOfVotesInt - 1;
                newIssueContainer.NumOfVotesInt = newVoteCount;
                newIssueContainer.NumOfVotes = newVoteCount.ToString();
                newIssueContainer.ListOfVoters.Remove(User.Identity.Name);
            }
            else
            {
                // Update vote count
                var newVoteCount = newIssueContainer.NumOfVotesInt + 1;
                newIssueContainer.NumOfVotesInt = newVoteCount;
                newIssueContainer.NumOfVotes = newVoteCount.ToString();
                newIssueContainer.ListOfVoters.Add(User.Identity.Name);
            }
            newIssueContainer.StringOfVoters = string.Join(",", newIssueContainer.ListOfVoters.ToArray()).Trim().TrimStart(',');

            // Update issue on GitHub
            var update = issue.ToUpdate();
            update.Body = newIssueContainer.Serialize();
            await _gitHubService.UpdateIssue(appName, id, update);

            TempData["voteValidationMessage"] = "Thank you for voting on this issue!";  

            // Update voters
            return RedirectToAction("details", "home", new { appName = appName, id = voteID });
        }

        /*
         * Displays the details of an issue along with all its comments
         * @param string voteID - The number of the issue we are upvoting
         */
        [HttpGet("/issues/{appName}/{id}")]
        public async Task<IActionResult> Details(string appName, int id)
        {
            // Getting issue
            var issue = await _gitHubService.GetIssue(appName, id);
            var newIssueContainer = new IssueContainer { Kerberos = User.Identity.Name };
            newIssueContainer.Deserialize(issue);

            // Getting all comments in the issue
            var issueComments = await _gitHubService.GetComments(appName, id);
            var listOfComments = new List<CommentContainer>();

            foreach (var i in issueComments) {

                var indexOfLine = i.Body.IndexOf ("--------------------");
                // add to the list if the issue is from FeedbackBot.
                if (indexOfLine >= 0) {

                    var bodycomment = i.Body.Substring (0, indexOfLine);
                    // don't add to the list if its an issue with empty comment.
                    if (!String.IsNullOrWhiteSpace (bodycomment)) {
                        var commentContainer = new CommentContainer ();
                        commentContainer.Deserialize (i, bodycomment);
                        listOfComments.Add (commentContainer);
                    }
                }
            }

            // Update model view
            var issuesView = new IssueDetailsViewModel
            {
                Comments = listOfComments,
                Issue = newIssueContainer
            };

            if (TempData["voteValidationMessage"] != null)
            {
                issuesView.VoteMessage = TempData["voteValidationMessage"].ToString();
            }
            ViewData["AppName"] = appName;

            return View(issuesView);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
