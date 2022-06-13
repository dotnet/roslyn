// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Options;

internal interface ILegacyWorkspaceOptionService : IWorkspaceService
{
    IGlobalOptionService GlobalOptions { get; }

    void RegisterWorkspace(Workspace workspace);
    void UnregisterWorkspace(Workspace workspace);

    object? GetOption(OptionKey key);
    void SetOptions(OptionSet optionSet, IEnumerable<OptionKey> optionKeys);
}
