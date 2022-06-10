// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Implementation.LanguageServer.Extensions
{
    internal static class SymbolExtensions
    {
        public static async Task<IEnumerable<TaggedText>> GetDescriptionAsync(this ISymbol symbol, TextDocument document, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var codeProject = document.GetCodeProject();
            var formatter = codeProject.LanguageServices.GetService<IDocumentationCommentFormattingService>();
            if (formatter == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var symbolDisplayService = codeProject.LanguageServices.GetService<ISymbolDisplayService>();
            if (symbolDisplayService == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            // Any code document will do
            var codeDocument = codeProject.Documents.FirstOrDefault();
            if (codeDocument == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var semanticModel = await codeDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return Enumerable.Empty<TaggedText>();
            }

            var services = codeProject.Solution.Workspace.Services;
            var quickInfo = await QuickInfoUtilities.CreateQuickInfoItemAsync(services, semanticModel, span: default, ImmutableArray.Create(symbol), options, cancellationToken).ConfigureAwait(false);
            var builder = new List<TaggedText>();
            foreach (var section in quickInfo.Sections)
            {
                if (builder.Any())
                {
                    builder.AddLineBreak();
                }

                builder.AddRange(section.TaggedParts);
            }

            return builder.ToImmutableArray();
        }
    }
}
