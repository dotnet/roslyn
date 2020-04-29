// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // Must cache using SymbolKey+ProjectId.  That's because the same symbol key may be found among many projects, but
    // the same operation on the same symbol key might produce different results depending on which project it was found
    // in.  For example, each symbol's project may have a different set of downstream dependent projects.  As such,
    // there may be a different set of related symbols found for each.

    internal static partial class DependentTypeFinder
    {
        private class KeyEqualityComparer : IEqualityComparer<(SymbolKey, ProjectId, IImmutableSet<Project>)>
        {
            public static readonly KeyEqualityComparer Instance = new KeyEqualityComparer();

            private KeyEqualityComparer()
            {
            }

            public bool Equals((SymbolKey, ProjectId, IImmutableSet<Project>) x,
                               (SymbolKey, ProjectId, IImmutableSet<Project>) y)
            {
                var (xSymbolKey, xProjectId, xProjects) = x;
                var (ySymbolKey, yProjectId, yProjects) = y;

                if (!xSymbolKey.Equals(ySymbolKey))
                    return false;

                if (!xProjectId.Equals(yProjectId))
                    return false;

                if (xProjects is null)
                    return yProjects is null;

                if (yProjects is null)
                    return false;

                return xProjects.SetEquals(yProjects);
            }

            public int GetHashCode((SymbolKey, ProjectId, IImmutableSet<Project>) obj)
            {
                var (symbolKey, projectId, projects) = obj;

                var projectsHash = 0;
                if (projects != null)
                {
                    foreach (var project in projects)
                        projectsHash += project.GetHashCode();
                }

                return Hash.Combine(symbolKey.GetHashCode(),
                       Hash.Combine(projectId, projectsHash));
            }
        }
    }
}
