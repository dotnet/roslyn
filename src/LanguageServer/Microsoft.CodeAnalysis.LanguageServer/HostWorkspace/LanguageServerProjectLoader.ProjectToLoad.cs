// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    /// <summary>
    /// The project path of the project to load.
    /// </summary>
    private sealed record ProjectToLoad(string Path, WorkDoneProgressTracker? ProgressTracker = null)
    {
        public static IEqualityComparer<ProjectToLoad> Comparer = new ProjectToLoadComparer();

        private sealed class ProjectToLoadComparer : IEqualityComparer<ProjectToLoad>
        {
            public bool Equals(ProjectToLoad? x, ProjectToLoad? y)
            {
                return PathUtilities.Comparer.Equals(x?.Path, y?.Path);
            }

            public int GetHashCode([DisallowNull] ProjectToLoad obj)
            {
                return PathUtilities.Comparer.GetHashCode(obj.Path);
            }
        }
    }
}
