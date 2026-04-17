// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.RemoteControl;

namespace Microsoft.VisualStudio.LanguageServices.Storage;

internal sealed class FileDownloader : IFileDownloader
{
    public sealed class Factory : IFileDownloaderFactory
    {
        public static readonly Factory Instance = new();

        public IFileDownloader CreateClient(string hostId, string serverPath, int pollingMinutes)
        {
            // BaseUrl provided by the VS RemoteControl client team.  This is URL we are supposed
            // to use to publish and access data from.
            const string BaseUrl = "https://aka.ms/vssettings/pub";

            return new FileDownloader(new RemoteControlClient(hostId, BaseUrl, serverPath, pollingMinutes));
        }
    }

    private readonly RemoteControlClient _client;

    private FileDownloader(RemoteControlClient client)
        => _client = client;

#if NET

    /// <summary>
    /// The netcore version of <see cref="RemoteControlClient"/> doesn't support ReturnStale.  It will download the file
    /// (on a separate thread), but then not cache it because it doesn't have access to the normal IE component that
    /// does proper header reading/caching.  Then, when we call in to read the file, we get nothing back, since nothing
    /// was cached.
    /// <para/> The temporary solution to this is to force the download to happen.  This is not ideal as we will no
    /// longer be respecting the server "Cache-Control:Max-Age" header.  Which means we'll continually download the
    /// files, even if not needed (since the server says to use the local value).  This is not great, but is not
    /// terrible either.  First, we will only download the full DB file <em>once</em>, when it is actually missing on
    /// the user's machine.  From that point on, we'll only be querying the server for the delta-patch file for the DB
    /// version we have locally.  The vast majority of the time that is a tiny document of the form <c><![CDATA[<Patch
    /// upToDate="true" FileVersion="105" ChangesetId="1CBE1453" />]]></c> (around 70 bytes) which simply tells the user
    /// they are up to date.  Only about once every three months will they actually download a large patch file.  Also,
    /// this patch download will only happen once a day tops (as that is our cadence for checking if there are new index
    /// versions out).
    /// <para/> https://github.com/dotnet/roslyn/issues/71014 tracks this issue.  Once RemoteControlClient is updated to
    /// support this again, we can remove this specialized code for netcore.
    /// </summary>
    public async Task<Stream?> ReadFileAsync()
        // Note: we try .ReturnStale first so this will automatically light up once they fix their issue, without 
        // us having to do anything on our end.  Once we do get around to making a change, we'll remove the 
        // .ForceDownload part entirely.
        => await _client.ReadFileAsync(BehaviorOnStale.ReturnStale).ConfigureAwait(false) ??
           await _client.ReadFileAsync(BehaviorOnStale.ForceDownload).ConfigureAwait(false);

#else

    public Task<Stream?> ReadFileAsync()
        => _client.ReadFileAsync(BehaviorOnStale.ReturnStale);

#endif

    public void Dispose()
        => _client.Dispose();
}
