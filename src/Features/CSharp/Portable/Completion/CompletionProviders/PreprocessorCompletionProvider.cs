// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(PreprocessorCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(SnippetCompletionProvider))]
    [Shared]
    internal class PreprocessorCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PreprocessorCompletionProvider()
        {
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        internal override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var originatingDocument = context.Document;
            var position = context.Position;

            var syntaxTree = await originatingDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true);
            var targetToken = leftToken.GetPreviousTokenIfTouchingWord(position);
            var isPreProcessorExpressionContext = targetToken.IsPreProcessorExpressionContext();
            if (!isPreProcessorExpressionContext)
                return;

            var solution = originatingDocument.Project.Solution;

            using var _ = PooledHashSet<string>.GetInstance(out var preprocessorNames);

            // Walk all the projects this document is linked in so that we get the full set of preprocessor symbols
            // defined across all of them.
            foreach (var documentId in solution.GetRelatedDocumentIds(originatingDocument.Id))
            {
                var document = solution.GetRequiredDocument(documentId);
                var currentSyntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                preprocessorNames.AddRange(currentSyntaxTree.Options.PreprocessorSymbolNames);
            }

            // Keep all the preprocessor symbol names together.  We don't want to intermingle them with any keywords we
            // include (like `true/false`)
            var order = 0;
            foreach (var name in preprocessorNames.OrderBy(a => a))
            {
                context.AddItem(CommonCompletionItem.Create(
                    name,
                    displayTextSuffix: "",
                    CompletionItemRules.Default,
                    glyph: Glyph.Keyword,
                    sortText: order.ToString("0000")));
                order++;
            }
        }
    }
}
