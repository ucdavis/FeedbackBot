using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Octokit;

namespace FeedbackBot.Models
{
    public class IssueContainer
    {
        public string? Title { get; set; }

        public string? NumOfVotes { get; set; }

        public int NumOfVotesInt { get; set; }

        public string? StringOfVoters { get; set; }

        public List<string> ListOfVoters { get; set; } = new ();

        public string? Body { get; set; }

        public int Number { get; set; }

        public string? VoteState { get; set; }

        public string? Kerberos { get; set; }

        public string? Author { get; set; }

        public int NumOfComments { get; set; }

        // Returns string of description for GitHub issue body
        public string Serialize()
        {
            return
                $"{Body}\r\n--------------------\r\n" +
                $"Votes: {NumOfVotes}\r\n" +
                $"Author: {Author}\r\n" +
                $"Voters: {StringOfVoters}";
        }

        public void Deserialize(Issue issue)
        {
            // Issue body, title, and issue number
            var issueBody = issue.Body;
            Title = issue.Title;
            Number = issue.Number;

            // Number of comments
            NumOfComments = issue.Comments;

            // Votes
            const string votesStringPattern = "(?<=Votes: )[0-9]+";
            var votesText = Regex.Match(issueBody, votesStringPattern);
            NumOfVotes = votesText.Value;
            NumOfVotesInt = int.Parse(NumOfVotes);

            //Author
            const string authorStringPattern = "(?<=Author: )[a-zA-Z]+";
            var authorText = Regex.Match(issueBody, authorStringPattern);
            Author = authorText.Value;

            // Feedback
            var indexOfLine = issueBody.IndexOf("--------------------");
            Body = issueBody.Substring(0, indexOfLine);

            // Voters
            var indexOfVoters = issueBody.IndexOf("Voters:");
            StringOfVoters = issueBody.Substring(indexOfVoters + 7);
            ListOfVoters = StringOfVoters.Split(',').Select(d => d.Trim()).ToList();

            VoteState = StringOfVoters.IndexOf(Kerberos ?? "", StringComparison.Ordinal) > 0 ? "unvote" : "vote";
        }
    }
}
