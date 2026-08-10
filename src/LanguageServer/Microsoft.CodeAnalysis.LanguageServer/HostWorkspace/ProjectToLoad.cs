// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// The project path (and the guid if it came from a solution) of the project to load.
/// </summary>
internal sealed record ProjectToLoad(
    string Path,
    LanguageServerProjectLoader.ProjectLoadOperation? LoadOperation,
    string? ProjectGuid,
    bool ReportTelemetry)
{
    public static IEqualityComparer<ProjectToLoad> Comparer = new ProjectToLoadComparer();

    private sealed class ProjectToLoadComparer : IEqualityComparer<ProjectToLoad>
    {
        public bool Equals(ProjectToLoad? x, ProjectToLoad? y)
        {
            if (!PathUtilities.Comparer.Equals(x?.Path, y?.Path))
                return false;

            if (x?.LoadOperation is null && y?.LoadOperation is null)
                return true;

            return ReferenceEquals(x?.LoadOperation, y?.LoadOperation);
        }

        public int GetHashCode([DisallowNull] ProjectToLoad obj)
        {
            return Hash.Combine(
                obj.LoadOperation is null ? 0 : RuntimeHelpers.GetHashCode(obj.LoadOperation),
                PathUtilities.Comparer.GetHashCode(obj.Path));
        }
    }
}
