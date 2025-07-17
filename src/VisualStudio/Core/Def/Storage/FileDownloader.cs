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

    public Task<Stream?> ReadFileAsync()
        => _client.ReadFileAsync(BehaviorOnStale.ReturnStale);

    public void Dispose()
        => _client.Dispose();
}
