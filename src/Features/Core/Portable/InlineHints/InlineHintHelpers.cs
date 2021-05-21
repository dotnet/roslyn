﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.InlineHints
{
    internal static class InlineHintHelpers
    {
        public static Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? GetDescriptionFunction(int position, SymbolKey symbolKey)
            => (document, cancellationToken) => GetDescriptionAsync(document, position, symbolKey, cancellationToken);

        private static async Task<ImmutableArray<TaggedText>> GetDescriptionAsync(Document document, int position, SymbolKey symbolKey, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var symbol = symbolKey.Resolve(semanticModel.Compilation, cancellationToken: cancellationToken).Symbol;
            if (symbol != null)
            {
                var workspace = document.Project.Solution.Workspace;
                var symbolDisplayService = document.GetRequiredLanguageService<ISymbolDisplayService>();

                var parts = new List<TaggedText>();

                var groups = await symbolDisplayService.ToDescriptionGroupsAsync(
                    workspace, semanticModel, position, ImmutableArray.Create(symbol), cancellationToken).ConfigureAwait(false);

                parts.AddRange(groups[SymbolDescriptionGroups.MainDescription]);

                var formatter = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();
                var documentation = symbol.GetDocumentationParts(semanticModel, position, formatter, cancellationToken);

                if (documentation.Any())
                {
                    parts.AddLineBreak();
                    parts.AddRange(documentation);
                }

                if (groups.TryGetValue(SymbolDescriptionGroups.AnonymousTypes, out var anonymousTypes))
                {
                    if (!anonymousTypes.IsDefaultOrEmpty)
                    {
                        parts.AddLineBreak();
                        parts.AddLineBreak();
                        parts.AddRange(anonymousTypes);
                    }
                }

                return parts.ToImmutableArray();
            }

            return default;
        }
    }
}
