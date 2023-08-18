// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;
using static CSharpCollectionExpressionRewriter;
using static CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer;

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
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

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
        var arguments = GetArguments(analysisResult.Matches);

        var dummyObjectAnnotation = new SyntaxAnnotation();
        var dummyObjectCreation = ImplicitObjectCreationExpression(ArgumentList(arguments), initializer: analysisResult.ExistingInitializer)
            .WithTriviaFrom(invocationExpression)
            .WithAdditionalAnnotations(dummyObjectAnnotation);

        var newSemanticDocument = await semanticDocument.WithSyntaxRootAsync(
            semanticDocument.Root.ReplaceNode(invocationExpression, dummyObjectCreation), cancellationToken).ConfigureAwait(false);
        dummyObjectCreation = (ImplicitObjectCreationExpressionSyntax)newSemanticDocument.Root.GetAnnotatedNodes(dummyObjectAnnotation).Single();
        var expressions = dummyObjectCreation.ArgumentList.Arguments.Select(a => a.Expression);
        var matches = expressions.Zip(analysisResult.Matches).SelectAsArray(static t => new CollectionExpressionMatch<ExpressionSyntax>(t.First, t.Second.UseSpread));

        var collectionExpression = await CreateCollectionExpressionAsync(
            newSemanticDocument.Document,
            fallbackOptions,
            dummyObjectCreation,
            matches,
            static o => o.Initializer,
            static (o, i) => o.WithInitializer(i),
            cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(invocationExpression, collectionExpression);

        static SeparatedSyntaxList<ArgumentSyntax> GetArguments(
            SourceText text, ImmutableArray<CollectionExpressionMatch<ExpressionSyntax>> matches)
        {
            using var _ = ArrayBuilder<SyntaxNodeOrToken>.GetInstance(out var nodesAndTokens);

            var comma = Token(SyntaxKind.CommaToken).WithoutTrivia().WithTrailingTrivia(Space);
            foreach (var (expression, _) in matches)
            {
                // After each expression, place a comma
                if (nodesAndTokens.Count == 0)
                    nodesAndTokens.Add(comma);

                // If the original 
                var firstToken = expression.GetFirstToken();
                if (text.AreOnSameLine(firstToken.GetPreviousToken(), firstToken))
                {
                    nodesAndTokens.Add(Argument(expression));
                }
                else
                {
                    nodesAndTokens.Add(Argument(expression.WithPrependedLeadingTrivia(CarriageReturnLineFeed)));
                }
            }

            return SeparatedList<ArgumentSyntax>(nodesAndTokens);
        }

#if false

        // Two major cases to handle.  First, the original expression was something like `new[] { 1, 2, 3
        // }.Concat(...).ToArray()` where there was an existing initializer we want to add the new elements to:

        if (analysisResult.ExistingInitializer != null)
        {

        }

        // We want to replace the final invocation (`builder.ToImmutable()`) with `new()`.  That way we can call into
        // the collection-rewriter to swap out that object-creation with the new collection-expression.

        // First, mark all the nodes we care about, so we can find them once we do the replacement with the
        // object-creation expression.
        var dummyObjectAnnotation = new SyntaxAnnotation();
        var newDocument = await CreateTrackedDocumentAsync(
            document, analysisResult, dummyObjectAnnotation, cancellationToken).ConfigureAwait(false);

        var root = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var dummyObjectCreation = (ImplicitObjectCreationExpressionSyntax)root.GetAnnotatedNodes(dummyObjectAnnotation).Single();

        // Move the original match over to this rewritten tree.
        analysisResult = TrackAnalysisResult(root, analysisResult);

        // Get the new collection expression.
        var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
            newDocument,
            fallbackOptions,
            dummyObjectCreation,
            analysisResult.Matches.SelectAsArray(m => new CollectionExpressionMatch<StatementSyntax>(m.Statement, m.UseSpread)),
            static o => o.Initializer,
            static (o, i) => o.WithInitializer(i),
            cancellationToken).ConfigureAwait(false);

        var subEditor = new SyntaxEditor(root, document.Project.Solution.Services);

        // Remove the actual declaration of the builder.
        subEditor.RemoveNode(analysisResult.LocalDeclarationStatement);

        // Remove all the nodes mutating the builder.
        foreach (var (statement, _) in analysisResult.Matches)
            subEditor.RemoveNode(statement);

        // Finally, replace the invocation where we convert the builder to a collection with the new collection expression.
        subEditor.ReplaceNode(dummyObjectCreation, collectionExpression);

        editor.ReplaceNode(editor.OriginalRoot, subEditor.GetChangedRoot());

        return;

        // Move the nodes in analysisResult over to the tracked result in the root passed in.
        static AnalysisResult TrackAnalysisResult(SyntaxNode root, AnalysisResult analysisResult)
            => new(//analysisResult.DiagnosticLocation,
                   analysisResult.ExistingInitializer is null ? null : root.GetCurrentNode(analysisResult.ExistingInitializer)!,
                   root.GetCurrentNode(analysisResult.CreationExpression)!,
                   analysisResult.Matches.SelectAsArray(m => new CollectionExpressionMatch<ExpressionSyntax>(root.GetCurrentNode(m.Node)!, m.UseSpread)));

        // Creates a new document with all of the relevant nodes in analysisResult tracked so that we can find them
        // across mutations we're making.
        static async Task<Document> CreateTrackedDocumentAsync(
            Document document,
            AnalysisResult analysisResult,
            SyntaxAnnotation annotation,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToTrack);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            nodesToTrack.AddIfNotNull(analysisResult.ExistingInitializer);
            nodesToTrack.Add(analysisResult.CreationExpression);
            foreach (var (statement, _) in analysisResult.Matches)
                nodesToTrack.Add(statement);

            var newRoot = root.TrackNodes(nodesToTrack);
            var creationExpression = newRoot.GetCurrentNode(analysisResult.CreationExpression)!;

            var dummyObjectCreation = ImplicitObjectCreationExpression()
                .WithTriviaFrom(creationExpression)
                .WithAdditionalAnnotations(annotation);

            var newDocument = document.WithSyntaxRoot(newRoot.ReplaceNode(creationExpression, dummyObjectCreation));
            return newDocument;
        }
#endif
    }
}
