﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IRemoteDependentTypeFinderService
    {
        ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindTypesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId type,
            ImmutableArray<ProjectId> projectsOpt,
            bool transitive,
            DependentTypesKind kind,
            CancellationToken cancellationToken);
    }
}
