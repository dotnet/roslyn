namespace DownloadNetSdkAnalyzers;

public static class HttpClientExtensions
{
    public static async Task<Stream> GetSeekableStreamAsync(this HttpClient client, string requestUriString)
    {
        var requestUri = new Uri(requestUriString);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Host = requestUri.Host;

        var response = await client.SendAsync(request);
        var stream = await response.Content.ReadAsStreamAsync();

        return stream;
    }
}
