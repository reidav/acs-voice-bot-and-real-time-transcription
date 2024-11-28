namespace Api.Services.Identity;

public interface IIdentityService
{
    string GetNewUserId();

    Task<(string, string)> GetNewUserIdAndToken();

    Task<string> GetTokenForUserId(string userId);
}