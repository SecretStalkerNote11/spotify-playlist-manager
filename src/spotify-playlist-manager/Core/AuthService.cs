using SpotifyAPI.Web;

namespace SpotifyPlaylistManager.Core;

/// <summary>
/// Handles OAuth 2.0 authentication with the Spotify Web API using the Authorization Code flow.
/// </summary>
public class AuthService
{
    private readonly string _clientId;
    private readonly string _clientSecret;

    public AuthService(string clientId, string clientSecret)
    {
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
    }

    /// <summary>
    /// Authenticates the user and returns a SpotifyClient instance.
    /// </summary>
    /// <returns>An authenticated SpotifyClient.</returns>
    public async Task<SpotifyClient> AuthenticateAsync()
    {
        var config = SpotifyClientConfig.CreateDefault();

        var request = new ClientCredentialsRequest(_clientId, _clientSecret);
        var response = await new OAuthClient(config).RequestToken(request);

        if (response.HasError())
        {
            throw new InvalidOperationException($"Authentication failed: {response.ErrorDescription}");
        }

        var spotify = new SpotifyClient(config.WithToken(response.AccessToken));
        return spotify;
    }
}
