// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    internal sealed record RemoteProjectLoadResult
    {
        public required ImmutableArray<ProjectFileInfo> ProjectFileInfos { get; init; }
        public required ImmutableArray<DiagnosticLogItem> DiagnosticLogItems { get; init; }
        public required string? ProjectRestorePath { get; init; }
        public required ProjectSystemProjectFactory ProjectFactory { get; init; }
        public required bool IsFileBasedProgram { get; init; }
        public required bool IsMiscellaneousFile { get; init; }
        public required bool HasFileBasedAppDirectives { get; init; }
        public required bool HasAllInformation { get; init; }
        public required BuildHostProcessKind PreferredBuildHostKind { get; init; }
        public required BuildHostProcessKind ActualBuildHostKind { get; init; }
    }
}
