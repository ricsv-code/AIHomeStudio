using AIHomeStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json; 
using System.Threading.Tasks;


// TODO: Clean up unused / sloppy code

public interface IAuthenticator
{
    Task AuthenticateRequest(HttpRequestMessage request);
}

public class ApiKeyAuthenticator : IAuthenticator
{
    private readonly string _apiKey;

    public ApiKeyAuthenticator(string apiKey)
    {
        _apiKey = apiKey;
    }

    public Task AuthenticateRequest(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return Task.CompletedTask;
    }
}

public class OAuth2Authenticator : IAuthenticator
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tokenEndpoint; 
    private readonly string _authorizationEndpoint; 

    private readonly HttpClient _oauthClient = new HttpClient();

    private string _accessToken;
    private DateTime _accessTokenExpiration;
    private string _refreshToken; 

    private string _codeVerifier;
    private string _codeChallenge;

    public OAuth2Authenticator(string clientId, string clientSecret, string tokenEndpoint, string authorizationEndpoint = null)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tokenEndpoint = tokenEndpoint;
        _authorizationEndpoint = authorizationEndpoint; 
    }

    public async Task AuthenticateRequest(HttpRequestMessage request)
    {
        if (string.IsNullOrEmpty(_accessToken) || IsTokenExpired())
        {
            await GetAccessToken();
        }
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }


    private string GenerateCodeVerifier()
    {

        byte[] randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }


    private string GenerateCodeChallenge(string codeVerifier)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] codeVerifierBytes = System.Text.Encoding.ASCII.GetBytes(codeVerifier);
            byte[] hashBytes = sha256.ComputeHash(codeVerifierBytes);
            return Convert.ToBase64String(hashBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }

    public string GetAuthorizationUrl()
    {
        if (string.IsNullOrEmpty(_authorizationEndpoint))
        {
            throw new InvalidOperationException("Authorization endpoint is not set.");
        }

        _codeVerifier = GenerateCodeVerifier();
        _codeChallenge = GenerateCodeChallenge(_codeVerifier);

        var authorizationUrl = $"{_authorizationEndpoint}?" +
                                 $"client_id={_clientId}&" +
                                 $"response_type=code&" +
                                 $"redirect_uri=YOUR_REDIRECT_URI&" + 
                                 $"code_challenge={_codeChallenge}&" +
                                 $"code_challenge_method=S256";

        return authorizationUrl;
    }

    private async Task GetAccessToken(string authorizationCode = null)
    {
        try
        {
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);

            var tokenRequestContent = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },  
                { "grant_type", "client_credentials" } 
            };

            if (authorizationCode != null) 
            {
                tokenRequestContent = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", authorizationCode },
                    { "redirect_uri", "YOUR_REDIRECT_URI" }, // redirect uri igen
                    { "code_verifier", _codeVerifier }
                };
            }

            tokenRequest.Content = new FormUrlEncodedContent(tokenRequestContent);

            var tokenResponse = await _oauthClient.SendAsync(tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

            using (JsonDocument document = JsonDocument.Parse(tokenResponseContent))
            {
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("access_token", out JsonElement accessTokenElement))
                {
                    _accessToken = accessTokenElement.GetString();
                }
                else
                {
                    Logger.Log("'access_token' not found in token response.", this);
                    throw new JsonException("'access_token' not found in token response.");
                }

                if (root.TryGetProperty("refresh_token", out JsonElement refreshTokenElement))
                {
                    _refreshToken = refreshTokenElement.GetString();
                }
                else
                {
                    _refreshToken = null; 
                }

                if (root.TryGetProperty("expires_in", out JsonElement expiresInElement) && expiresInElement.TryGetInt32(out int expiresIn))
                {
                    _accessTokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn);
                }
                else
                {
                    _accessTokenExpiration = DateTime.UtcNow.AddHours(1);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error getting access token: {ex}", this);
            throw;
        }
    }


    public async Task RefreshAccessToken()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            throw new InvalidOperationException("Refresh token is not available.");
        }

        try
        {
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
            tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", _refreshToken },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            });

            var tokenResponse = await _oauthClient.SendAsync(tokenRequest);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            using (JsonDocument document = JsonDocument.Parse(tokenResponseContent))
            {
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("access_token", out JsonElement accessTokenElement))
                {
                    _accessToken = accessTokenElement.GetString();
                }
                else
                {
                    Logger.Log("'access_token' not found in token response.", this);
                    throw new JsonException("'access_token' not found in token response.");
                }

                if (root.TryGetProperty("refresh_token", out JsonElement refreshTokenElement))
                {
                    _refreshToken = refreshTokenElement.GetString();
                }
                else
                {
                    _refreshToken = null; 
                }

                if (root.TryGetProperty("expires_in", out JsonElement expiresInElement) && expiresInElement.TryGetInt32(out int expiresIn))
                {
                    _accessTokenExpiration = DateTime.UtcNow.AddSeconds(expiresIn);
                }
                else
                {
                    _accessTokenExpiration = DateTime.UtcNow.AddHours(1);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Error refreshing access token: {ex}", this);
            throw;
        }
    }

    private bool IsTokenExpired()
    {
        return DateTime.UtcNow >= _accessTokenExpiration;
    }
}