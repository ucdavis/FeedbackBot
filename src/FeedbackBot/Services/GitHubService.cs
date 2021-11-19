using Microsoft.Extensions.Options;
using Octokit;

namespace FeedbackBot.Services;

public interface IGitHubService
{
    Task<IEnumerable<Issue>> GetIssues(string appName);

    Task<Issue> CreateIssue(string appName, NewIssue issue);

    Task<Issue> GetIssue(string appName, int id);

    Task UpdateIssue(string appName, int id, IssueUpdate update);

    Task<IEnumerable<IssueComment>> GetComments(string appName, int issueId);

    Task<IssueComment> AddComment(string appName, int issueId, string comment);

    Task<SearchIssuesResult> Search(string appName, string query);
}

public class GitHubService : IGitHubService
{
    public GitHubClient client;

    public GitHubService(IOptions<AppSettings> appSettings)
    {
        var settings = appSettings.Value;

        client = new GitHubClient(new ProductHeaderValue("FeedbackBot"));

        client.Credentials = new Credentials(settings.GitHubToken);
    }

    public async Task<IEnumerable<Issue>> GetIssues(string appName)
    {
        var settings = new RepositoryIssueRequest
        {
            Filter = IssueFilter.Created,
            Labels = { "feedback" }
        };

        return await client.Issue.GetAllForRepository("ucdavis", appName, settings);
    }

    public async Task<Issue> CreateIssue(string appName, NewIssue issue)
    {
        return await client.Issue.Create("ucdavis", appName, issue);
    }

    public async Task<Issue> GetIssue(string appName, int id)
    {
        return await client.Issue.Get("ucdavis", appName, id);
    }

    public Task UpdateIssue(string appName, int id, IssueUpdate update)
    {
        return client.Issue.Update("ucdavis", appName, id, update);
    }

    public async Task<IEnumerable<IssueComment>> GetComments(string appName, int issueId)
    {
        return await client.Issue.Comment.GetAllForIssue("ucdavis", appName, issueId);
    }

    public async Task<IssueComment> AddComment(string appName, int issueId, string comment)
    {
        return await client.Issue.Comment.Create("ucdavis", appName, issueId, comment);
    }

    public async Task<SearchIssuesResult> Search(string appName, string query)
    {
        var request = new SearchIssuesRequest(query);
        request.Repos.Add("ucdavis", appName);
        request.Labels = new List<string>() { "feedback" }; ;
        request.State = ItemState.Open;

        return await client.Search.SearchIssues(request);
    }
}

