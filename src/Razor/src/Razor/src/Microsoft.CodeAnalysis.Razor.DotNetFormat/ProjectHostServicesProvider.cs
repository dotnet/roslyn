// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed class ProjectHostServicesProvider(HostServices? hostServices) : IHostServicesProvider
{
    private HostServices? _hostServices = hostServices;

    public void SetHostServices(HostServices? hostServices)
        => _hostServices = hostServices;

    public HostServices GetServices()
        => _hostServices ?? throw new InvalidOperationException("Host services must be set before formatting.");
}
