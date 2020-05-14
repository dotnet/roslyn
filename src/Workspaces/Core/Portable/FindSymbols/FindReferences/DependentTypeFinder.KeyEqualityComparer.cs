// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DependentTypeFinder
    {
        /// <summary>
        /// Special comparer we need for our cache keys.  Necessary because <see cref="IImmutableSet{T}"/> uses
        /// reference equality and not value-equality semantics.
        /// </summary>
        private class KeyEqualityComparer : IEqualityComparer<(SymbolKey, ProjectId?, IImmutableSet<Project>)>
        {
            public static readonly KeyEqualityComparer Instance = new KeyEqualityComparer();

            private KeyEqualityComparer()
            {
            }

            public bool Equals((SymbolKey, ProjectId?, IImmutableSet<Project>) x,
                               (SymbolKey, ProjectId?, IImmutableSet<Project>) y)
            {
                var (xSymbolKey, xProjectId, xProjects) = x;
                var (ySymbolKey, yProjectId, yProjects) = y;

                if (!xSymbolKey.Equals(ySymbolKey))
                    return false;

                if (!Equals(xProjectId, yProjectId))
                    return false;

                if (xProjects is null)
                    return yProjects is null;

                if (yProjects is null)
                    return false;

                return xProjects.SetEquals(yProjects);
            }

            public int GetHashCode((SymbolKey, ProjectId?, IImmutableSet<Project>) obj)
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
