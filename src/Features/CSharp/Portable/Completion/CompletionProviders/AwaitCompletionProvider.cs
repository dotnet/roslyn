// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    /// <summary>
    /// A completion provider for offering <see langword="await"/> keyword.
    /// This is implemented separately, not as a keyword recommender as it contains extra logic for making container method async.
    /// </summary>
    /// <remarks>
    /// The container is made async if and only if the containing method is returning a Task-like type.
    /// </remarks>
    [ExportCompletionProvider(nameof(AwaitCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(KeywordCompletionProvider))]
    [Shared]
    internal sealed class AwaitCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AwaitCompletionProvider()
        {
        }

        public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharactersWithArgumentList;

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var workspace = document.Project.Solution.Workspace;
            var syntaxContext = CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken);
            if (!syntaxContext.IsAwaitKeywordContext(position))
            {
                return;
            }

            var method = syntaxContext.TargetToken.GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
            var shouldMakeContainerAsync = method is not null && !method.GetModifiers().Any(SyntaxKind.AsyncKeyword);
            var completionItem = CommonCompletionItem.Create(
                displayText: SyntaxFacts.GetText(SyntaxKind.AwaitKeyword),
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Keyword,
                description: RecommendedKeyword.CreateDisplayParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), string.Empty),
                inlineDescription: shouldMakeContainerAsync ? CSharpFeaturesResources.Make_container_async : null,
                isComplexTextEdit: shouldMakeContainerAsync);
            context.AddItem(completionItem);
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            // IsComplexTextEdit is true when we want to add async to the container.
            if (!item.IsComplexTextEdit)
            {
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = root.FindToken(item.Span.Start).GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
            if (declaration is null)
            {
                // We already check that in ProvideCompletionsAsync above.
                Debug.Assert(false, "Expected non-null value for declaration.");
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);
            builder.Add(new TextChange(new TextSpan(GetSpanStart(declaration), 0), "async "));
            builder.Add(new TextChange(item.Span, item.DisplayText));

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(builder);
            return CompletionChange.Create(CodeAnalysis.Completion.Utilities.Collapse(newText, builder.ToImmutableArray()));
        }

        /// <summary>
        /// Gets the span start where async keyword should go.
        /// </summary>
        private static int GetSpanStart(SyntaxNode declaration)
        {
            return declaration switch
            {
                MethodDeclarationSyntax method => method.ReturnType.SpanStart,
                LocalFunctionStatementSyntax local => local.ReturnType.SpanStart,
                AnonymousMethodExpressionSyntax anonymous => anonymous.DelegateKeyword.SpanStart,
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.SpanStart,
                SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.SpanStart,
                _ => throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
            };
        }
    }
}
