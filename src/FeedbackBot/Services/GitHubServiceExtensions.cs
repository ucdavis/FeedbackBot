using FeedbackBot.Models;
using Octokit;

namespace FeedbackBot.Services;

public static class GitHubServiceExtensions
{
    public static CommentContainer? ToIssueCommentContainer(this IssueComment comment)
    {
        var indexOfLine = comment.Body.IndexOf("--------------------");
        if (indexOfLine == -1)
        {
            // not a FeedbackBot comment
            return null;
        }

        var bodycomment = comment.Body.Substring(0, indexOfLine);
        if (String.IsNullOrWhiteSpace(bodycomment))
        {
            return null;
        }

        var commentContainer = new CommentContainer();
        commentContainer.Deserialize(comment, bodycomment);
        return commentContainer;
    }
}
