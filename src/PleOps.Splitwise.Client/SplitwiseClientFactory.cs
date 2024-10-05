namespace PleOps.Splitwise.Client;

using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Factory to create clients of the Splitewise API.
/// </summary>
public static class SplitwiseClientFactory
{
    /// <summary>
    /// Create a new client with the api key authentication.
    /// </summary>
    /// <param name="apiKey">Personal API key for authentication.</param>
    /// <param name="options">Options for the client.</param>
    /// <returns>New client for the Splitewise API.</returns>
    public static SplitwiseClient CreateWithApiKeyAuth(string apiKey, SplitwiseClientOptions? options = null)
    {
        options ??= new SplitwiseClientOptions();
        IRequestOption[] httpOptions = [ options.RetryOptions ];

        // Set the base address in the http client if required.
        // In this case the default value comes from the openapi spec.
        HttpClient httpClient = KiotaClientFactory.Create(null, httpOptions);

        var tokenProvider = new ApiKeyAccessTokenProvider(apiKey);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);

        return new SplitwiseClient(adapter);
    }

    private sealed class ApiKeyAccessTokenProvider(string apiKey) : IAccessTokenProvider
    {
        public AllowedHostsValidator AllowedHostsValidator => throw new NotImplementedException();

        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(apiKey);
        }
    }
}
