// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Only used by <see cref="Workspace"/> and <see cref="LegacySolutionOptionSet"/> to implement legacy public APIs:
/// <see cref="Workspace.Options"/> and <see cref="Solution.Options"/>.
/// </summary>
internal interface ILegacyWorkspaceOptionService : IWorkspaceService
{
    ILegacyGlobalOptionService LegacyGlobalOptions { get; }
}

internal interface ILegacyGlobalOptionService
{
    IGlobalOptionService GlobalOptions { get; }

    void RegisterWorkspace(Workspace workspace);
    void UnregisterWorkspace(Workspace workspace);
    void UpdateRegisteredWorkspaces();

    object? GetExternallyDefinedOption(OptionKey key);

    void SetOptions(
        ImmutableArray<KeyValuePair<OptionKey2, object?>> internallyDefinedOptions,
        ImmutableArray<KeyValuePair<OptionKey, object?>> externallyDefinedOptions);
}
