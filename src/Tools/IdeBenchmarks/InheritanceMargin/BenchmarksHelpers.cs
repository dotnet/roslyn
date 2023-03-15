// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace IdeBenchmarks.InheritanceMargin
{
    internal static class BenchmarksHelpers
    {
        public static async Task<ImmutableArray<InheritanceMarginItem>> GenerateInheritanceMarginItemsAsync(
            Solution solution,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<InheritanceMarginItem>.GetInstance(out var builder);
            foreach (var project in solution.Projects)
            {
                var languageService = project.GetRequiredLanguageService<IInheritanceMarginService>();
                foreach (var document in project.Documents)
                {
                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var items = await languageService.GetInheritanceMemberItemsAsync(
                        document, root.Span, includeGlobalImports: true, frozenPartialSemantics: true, cancellationToken).ConfigureAwait(false);
                    builder.AddRange(items);
                }
            }

            return builder.ToImmutable();
        }
    }
}
