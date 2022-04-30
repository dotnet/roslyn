﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Client-side object that is called back from the server when options for a certain language are required.
/// Can be used when the remote API does not have an existing callback. If it does it can implement 
/// <see cref="GetOptionsAsync(string, CancellationToken)"/> itself.
/// </summary>
internal sealed class RemoteOptionsProvider<TOptions>
{
    private readonly HostWorkspaceServices _services;
    private readonly Func<HostLanguageServices, CancellationToken, ValueTask<TOptions>> _optionsProvider;

    public RemoteOptionsProvider(HostWorkspaceServices services, Func<HostLanguageServices, CancellationToken, ValueTask<TOptions>> optionsProvider)
    {
        _services = services;
        _optionsProvider = optionsProvider;
    }

    internal ValueTask<TOptions> GetOptionsAsync(string language, CancellationToken cancellationToken)
        => _optionsProvider(_services.GetLanguageServices(language), cancellationToken);
}
