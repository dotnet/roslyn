// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceService(typeof(IWorkspaceConfigurationService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceConfigurationService(IGlobalOptionService globalOptions) : IWorkspaceConfigurationService
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    [AllowNull]
    public WorkspaceConfigurationOptions Options { get => field ??= _globalOptions.GetWorkspaceConfigurationOptions(); private set; }

    internal void Clear()
        => Options = null;
}
