// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// The project path (and the guid if it game from a solution) of the project to load.
/// </summary>
internal record ProjectToLoad(string Path, string? ProjectGuid)
{
    public static IEqualityComparer<ProjectToLoad> Comparer = new ProjectToLoadComparer();

    private class ProjectToLoadComparer : IEqualityComparer<ProjectToLoad>
    {
        public bool Equals(ProjectToLoad? x, ProjectToLoad? y)
        {
            return StringComparer.Ordinal.Equals(x?.Path, y?.Path);
        }

        public int GetHashCode([DisallowNull] ProjectToLoad obj)
        {
            return StringComparer.Ordinal.GetHashCode(obj.Path);
        }
    }
}
