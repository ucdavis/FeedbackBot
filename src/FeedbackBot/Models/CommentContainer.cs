using System;
using Octokit;

namespace FeedbackBot.Models
{
    public class CommentContainer
    {
        private const string FeedbackBotSeparator = "--------------------";
        private const string FeedbackBotAuthorPrefix = "Author:";
        private const string FallbackAuthor = "Admin";

        public string Body { get; set; }

        public string Author { get; set; }

        public string CreateDate { get; set; }

        public void Deserialize(IssueComment comment)
        {
            var indexOfLine = comment.Body.IndexOf(FeedbackBotSeparator);
            var bodyComment = indexOfLine >= 0
                ? comment.Body.Substring(0, indexOfLine)
                : comment.Body;

            Body = bodyComment.Trim();
        
            var indexOfAuthor = comment.Body.IndexOf(FeedbackBotAuthorPrefix);

            if (indexOfAuthor >= 0)
            {
                Author = comment.Body.Substring(indexOfAuthor + FeedbackBotAuthorPrefix.Length).Trim();
            }
            else
            {
                Author = string.IsNullOrWhiteSpace(comment.User?.Login) ? FallbackAuthor : comment.User.Login;
            }

            if (string.IsNullOrWhiteSpace(Author))
            {
                Author = FallbackAuthor;
            }

            CreateDate = comment.CreatedAt.ToString().Remove(9);
        }
    }
}
