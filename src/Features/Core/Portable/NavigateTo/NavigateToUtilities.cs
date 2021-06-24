// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal static class NavigateToUtilities
    {
        public static ImmutableHashSet<string> GetKindsProvided(Solution solution)
        {
            var result = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
            foreach (var project in solution.Projects)
            {
                var navigateToSearchService = project.GetLanguageService<INavigateToSearchService>();
                if (navigateToSearchService != null)
                    result.UnionWith(navigateToSearchService.KindsProvided);
            }

            return result.ToImmutable();
        }
    }
}
