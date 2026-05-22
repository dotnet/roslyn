// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal interface IHostServicesProvider
{
    HostServices GetServices();
}
