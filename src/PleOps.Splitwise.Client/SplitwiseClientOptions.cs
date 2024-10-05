namespace PleOps.Splitwise.Client;

using System.Net;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

/// <summary>
/// Options for the Splitwise client.
/// </summary>
public class SplitwiseClientOptions
{
    /// <summary>
    /// Gets or sets the options for the retry handler of the client.
    /// </summary>
    public RetryHandlerOption RetryOptions { get; set; } = new() {
        ShouldRetry = (_, _, r) =>
            r.StatusCode is >= HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout,
    };
}
