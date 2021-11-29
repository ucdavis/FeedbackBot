using Microsoft.AspNetCore.Components.Authorization;

namespace FeedbackBot.Services;

public class UserService: IUserService
{
    private AuthenticationStateProvider _authStateProvider;

    public UserService(AuthenticationStateProvider authStateProvider) => _authStateProvider = authStateProvider;

    public async Task<string?> GetKerberos()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState?.User?.Identity?.Name;
    }

    public async Task<bool> IsAuthenticated()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState?.User?.Identity?.IsAuthenticated ?? false;
    }
}

public interface IUserService
{
    Task<string?> GetKerberos();
    Task<bool> IsAuthenticated();
}