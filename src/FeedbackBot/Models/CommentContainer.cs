using System;
using Octokit;

namespace FeedbackBot.Models
{
    public class CommentContainer
    {
        public string Body { get; set; }

        public string Author { get; set; }

        public string CreateDate { get; set; }

        public void Deserialize(IssueComment comment)
        {
            var indexOfLine = comment.Body.IndexOf("--------------------");
            Body = comment.Body.Substring(0, indexOfLine);

            var indexOfAuthor = comment.Body.IndexOf("Author:");
            Author = comment.Body.Substring(indexOfAuthor + 7);
            CreateDate = comment.CreatedAt.ToString().Remove(9);
        }
    }
}
