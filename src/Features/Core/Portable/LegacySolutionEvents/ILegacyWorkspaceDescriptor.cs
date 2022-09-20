// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents
{
    internal interface ILegacyWorkspaceDescriptor
    {
        /// <inheritdoc cref="Workspace.Kind"/>
        string? WorkspaceKind { get; }
        /// <inheritdoc cref="HostWorkspaceServices.SolutionServices"/>
        SolutionServices SolutionServices { get; }
        /// <inheritdoc cref="Workspace.CurrentSolution"/>
        Solution CurrentSolution { get; }
    }
}
