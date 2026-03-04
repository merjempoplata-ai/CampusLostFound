namespace CampusLostAndFound.Tests.Helpers;

/// <summary>
/// Minimal HttpMessageHandler stub. Default constructor produces a handler that throws,
/// causing EmbeddingService.EmbedAsync to fail (which is swallowed by TrySetEmbeddingAsync).
/// Pass an explicit response factory for tests that need a successful HTTP response.
/// </summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;

    /// <summary>Creates a handler that always throws HttpRequestException.</summary>
    public FakeHttpMessageHandler() { }

    /// <summary>Creates a handler that returns the result of <paramref name="responseFactory"/>.</summary>
    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        => _responseFactory = responseFactory;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_responseFactory is null)
            throw new HttpRequestException("Simulated HTTP failure from FakeHttpMessageHandler.");

        return Task.FromResult(_responseFactory(request));
    }
}
