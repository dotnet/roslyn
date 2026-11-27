// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Settings;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteClientSettingsService : IRemoteJsonService
{
    ValueTask UpdateAsync(ClientSettings settings, CancellationToken cancellationToken);
}
