using System.Net.WebSockets;
using Polly;
using Polly.Retry;
using RestSharp;

namespace Trader.Services;

public static class Reconnect
{
    public static AsyncRetryPolicy<RestResponse> GetRestPolicy()
    {
        return Policy.Handle<HttpRequestException>().OrResult<RestResponse>(r =>
                !r.IsSuccessful && (int)r.StatusCode >= 500 && (int)r.StatusCode < 600)
            .WaitAndRetryAsync(retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    public static AsyncRetryPolicy GetWcPolicy()
    {
        return Policy
            .Handle<WebSocketException>()
            .Or<Exception>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
            );
    }
}