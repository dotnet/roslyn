// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct SolutionUpdate
    {
        public readonly ManagedModuleUpdates ModuleUpdates;
        public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> NonRemappableRegions;
        public readonly ImmutableArray<IDisposable> ModuleReaders;
        public readonly ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> EmitBaselines;
        public readonly ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> Diagnostics;

        public SolutionUpdate(
            ManagedModuleUpdates moduleUpdates,
            ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions,
            ImmutableArray<IDisposable> moduleReaders,
            ImmutableArray<(ProjectId ProjectId, EmitBaseline Baseline)> emitBaselines,
            ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> diagnostics)
        {
            ModuleUpdates = moduleUpdates;
            NonRemappableRegions = nonRemappableRegions;
            EmitBaselines = emitBaselines;
            ModuleReaders = moduleReaders;
            Diagnostics = diagnostics;
        }

        public static SolutionUpdate Blocked()
            => Blocked(ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)>.Empty);

        public static SolutionUpdate Blocked(ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> diagnostics) => new(
            new(ManagedModuleUpdateStatus.Blocked, ImmutableArray<ManagedModuleUpdate>.Empty),
            ImmutableArray<(Guid, ImmutableArray<(ManagedModuleMethodId, NonRemappableRegion)>)>.Empty,
            ImmutableArray<IDisposable>.Empty,
            ImmutableArray<(ProjectId, EmitBaseline)>.Empty,
            diagnostics);
    }
}
