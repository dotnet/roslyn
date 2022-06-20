// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Options;

[ExportWorkspaceService(typeof(ILegacyWorkspaceOptionService)), Shared]
internal sealed class LegacyWorkspaceOptionService : ILegacyWorkspaceOptionService
{
    public IGlobalOptionService GlobalOptions { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LegacyWorkspaceOptionService(IGlobalOptionService globalOptionService)
        => GlobalOptions = globalOptionService;

    public void RegisterWorkspace(Workspace workspace)
        => GlobalOptions.RegisterWorkspace(workspace);

    public void UnregisterWorkspace(Workspace workspace)
        => GlobalOptions.UnregisterWorkspace(workspace);

    public object? GetOption(OptionKey key)
        => GlobalOptions.GetOption(key);

    public void SetOptions(OptionSet optionSet, IEnumerable<OptionKey> optionKeys)
        => GlobalOptions.SetOptions(optionSet, optionKeys);
}
