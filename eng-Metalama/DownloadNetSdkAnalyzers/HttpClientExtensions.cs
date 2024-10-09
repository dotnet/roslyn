namespace DownloadNetSdkAnalyzers;

public static class HttpClientExtensions
{
    public static async Task<Stream> GetSeekableStreamAsync(this HttpClient client, string requestUri)
    {
        var bytes = await client.GetByteArrayAsync(requestUri).ConfigureAwait(false);

        return new MemoryStream(bytes, writable: false);
    }
}
