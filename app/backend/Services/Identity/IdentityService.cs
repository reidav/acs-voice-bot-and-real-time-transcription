namespace Api.Services.Identity;

public class IdentityService : IIdentityService
{
    private readonly CommunicationIdentityClient client;

    public IdentityService(IConfiguration configuration)
    {
        var acsConnectionString = configuration["AcsConnectionString"];
        ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);
        client = new CommunicationIdentityClient(acsConnectionString);
    }

    public string GetNewUserId()
    {
        var identityResponse = client.CreateUser();
        return identityResponse.Value.ToString();
    }

    public async Task<(string, string)> GetNewUserIdAndToken()
    {
        var identityResponse = await client.CreateUserAndTokenAsync(
            scopes: new[] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP });
        return (identityResponse.Value.User.Id, identityResponse.Value.AccessToken.Token);
    }

    public async Task<string> GetTokenForUserId(string userId)
    {
        var identityResponse = await client.GetTokenAsync(
            new CommunicationUserIdentifier(userId),
            scopes: new[] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP });
        return identityResponse.Value.Token;
    }
}