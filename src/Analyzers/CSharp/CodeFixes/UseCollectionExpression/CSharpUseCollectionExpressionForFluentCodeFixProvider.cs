﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static CSharpCollectionExpressionRewriter;
using static CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForFluent), Shared]
internal partial class CSharpUseCollectionExpressionForFluentCodeFixProvider
    : ForkingSyntaxEditorBasedCodeFixProvider<InvocationExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForFluentCodeFixProvider()
        : base(CSharpCodeFixesResources.Use_collection_expression,
               IDEDiagnosticIds.UseCollectionExpressionForFluentDiagnosticId)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForFluentDiagnosticId);

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        InvocationExpressionSyntax invocationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var state = new UpdateExpressionState<ExpressionSyntax, StatementSyntax>(
            semanticModel, syntaxFacts, invocationExpression, valuePattern: default, initializedSymbol: null);

        if (AnalyzeInvocation(state, invocationExpression, addMatches: true, cancellationToken) is not { } analysisResult)
            return;

        // We want to replace `new[] { 1, 2, 3 }.Concat(x).Add(y).ToArray()` with the new collection expression.  To do
        // this, we go through the following steps.  First, we replace the whole expression with `new(x, y) { 1, 2, 3 }`
        // (a dummy object creation expression). We then call into our helper which replaces expressions with
        // collection expressions.  The reason for the dummy object creation expression is that it serves as an actual
        // node the rewriting code can attach an initializer to, by which it can figure out appropriate wrapping and
        // indentation for the collection expression elements.

        var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Get the expressions that we're going to fill the new collection expression with.
        var arguments = await GetArgumentsAsync(document, fallbackOptions, analysisResult.Matches, cancellationToken).ConfigureAwait(false);

        var argumentListTrailingTrivia = analysisResult.ExistingInitializer is null
            ? default
            : analysisResult.ExistingInitializer.GetFirstToken().GetPreviousToken().TrailingTrivia;

        var dummyObjectAnnotation = new SyntaxAnnotation();
        var dummyObjectCreation = ImplicitObjectCreationExpression(
                ArgumentList(arguments).WithTrailingTrivia(argumentListTrailingTrivia),
                initializer: analysisResult.ExistingInitializer)
            .WithTriviaFrom(invocationExpression)
            .WithAdditionalAnnotations(dummyObjectAnnotation);

        var newSemanticDocument = await semanticDocument.WithSyntaxRootAsync(
            semanticDocument.Root.ReplaceNode(invocationExpression, dummyObjectCreation), cancellationToken).ConfigureAwait(false);
        dummyObjectCreation = (ImplicitObjectCreationExpressionSyntax)newSemanticDocument.Root.GetAnnotatedNodes(dummyObjectAnnotation).Single();
        var expressions = dummyObjectCreation.ArgumentList.Arguments.Select(a => a.Expression);
        var matches = expressions
            .Zip(analysisResult.Matches, static (expression, match) => new CollectionExpressionMatch<ExpressionSyntax>(expression, match.UseSpread))
            .ToImmutableArray();

        var collectionExpression = await CreateCollectionExpressionAsync(
            newSemanticDocument.Document,
            fallbackOptions,
            dummyObjectCreation,
            matches,
            static o => o.Initializer,
            static (o, i) => o.WithInitializer(i),
            cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(invocationExpression, collectionExpression);

        static async Task<SeparatedSyntaxList<ArgumentSyntax>> GetArgumentsAsync(
            Document document,
            CodeActionOptionsProvider fallbackOptions,
            ImmutableArray<CollectionExpressionMatch<ArgumentSyntax>> matches,
            CancellationToken cancellationToken)
        {
            if (matches.IsEmpty)
                return default;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
#if CODE_STYLE
            var formattingOptions = SyntaxFormattingOptions.CommonDefaults;
#else
            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(
                fallbackOptions, cancellationToken).ConfigureAwait(false);
#endif

            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            // Get the first argument.  If it was originally like `Add(arg)` then keep it in that form in `new(arg)`.
            // However, if it was on it's own line originally, then preserve that in the new form as well.
            AddOriginallyFirstArgument(matches[0].Node);

            // Now go through and add the rest of the arguments.
            for (int i = 1, n = matches.Length; i < n; i++)
            {
                var argument = matches[i].Node;
                var argumentList = (ArgumentListSyntax)argument.GetRequiredParent();
                var originalArgumentListChildren = argumentList.Arguments.GetWithSeparators();
                var index = originalArgumentListChildren.IndexOf(argument);

                // if this was not the first argument in its original list.  for example: `.Add(1, 2, 3)`, then add its
                // preceding comma as well.  That way we preserve its original relationship in the rewritten code.
                if (index > 0)
                {
                    nodesAndTokens.Add(originalArgumentListChildren[index - 1]);
                    nodesAndTokens.Add(argument);
                }
                else
                {
                    nodesAndTokens.Add(Token(SyntaxKind.CommaToken).WithoutTrivia());
                    AddOriginallyFirstArgument(argument);
                }
            }

            return SeparatedList<ArgumentSyntax>(nodesAndTokens);

            void AddOriginallyFirstArgument(ArgumentSyntax firstArgument)
            {
                var firstToken = firstArgument.GetFirstToken();
                if (text.AreOnSameLine(firstToken.GetPreviousToken(), firstToken))
                {
                    nodesAndTokens.Add(firstArgument);
                }
                else
                {
                    nodesAndTokens.Add(firstArgument.WithPrependedLeadingTrivia(EndOfLine(formattingOptions.NewLine)));
                }
            }
        }
    }
}
