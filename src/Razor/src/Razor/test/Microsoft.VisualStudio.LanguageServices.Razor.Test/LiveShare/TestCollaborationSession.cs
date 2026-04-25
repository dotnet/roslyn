// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Razor.LiveShare;

public class TestCollaborationSession : CollaborationSession
{
    private readonly bool _isHost;

    public TestCollaborationSession(bool isHost)
    {
        _isHost = isHost;
    }

    /// <summary>
    /// Assumes that host paths are prefixed with /host and guest paths are prefixed with /guest.
    /// Converts such paths to vsls: uris
    /// </summary>
    public override Uri ConvertLocalPathToSharedUri(string localPath)
    {
        var path = localPath.Replace("/host", "").Replace("/guest", "");
        return new Uri($"vsls:{path}");
    }

    /// <summary>
    /// Assumes that host paths are prefixed with /host and guest paths are prefixed with /guest.
    /// Converts vsls: uris to such paths.
    /// </summary>
    public override string ConvertSharedUriToLocalPath(Uri uri)
    {
        var path = uri.ToString().Replace("vsls:", "");
        return _isHost ? $"/host{path}" : $"/guest{path}";
    }

    public override string SessionId => throw new NotImplementedException();
    public override IReadOnlyCollection<Peer> Peers => throw new NotImplementedException();
    public override IReadOnlyCollection<string> RemoteServiceNames => throw new NotImplementedException();
    public override int PeerNumber => throw new NotImplementedException();
    public override PeerIdentity Identity => throw new NotImplementedException();
    public override PeerRole Role => throw new NotImplementedException();
    public override PeerAccess Access => throw new NotImplementedException();

    public override Task<string> DownloadFileAsync(Uri uri, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task<TService> GetRemoteServiceAsync<TService>(string name, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override object GetService(Type serviceType)
    {
        if (serviceType.Name == "JsonSerializer")
        {
            return new JsonSerializer();
        }

        return null;
    }

    public override Task<Uri[]> ListRootsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override Task<Uri[]> ListDirectoryAsync(Uri uri, ListDirectoryOptions options, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
