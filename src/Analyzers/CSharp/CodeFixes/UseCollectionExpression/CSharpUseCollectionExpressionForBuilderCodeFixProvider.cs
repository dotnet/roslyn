// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static CSharpCollectionExpressionRewriter;
using static CSharpUseCollectionExpressionForBuilderDiagnosticAnalyzer;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForBuilder), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class CSharpUseCollectionExpressionForBuilderCodeFixProvider()
    : AbstractUseCollectionExpressionCodeFixProvider<InvocationExpressionSyntax>(
        CSharpCodeFixesResources.Use_collection_expression,
        IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId)
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId];

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        InvocationExpressionSyntax invocationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var expressionType = semanticModel.Compilation.ExpressionOfTType();
        if (AnalyzeInvocation(semanticModel, invocationExpression, expressionType, allowSemanticsChange: true, cancellationToken) is not { } analysisResult)
            return;

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
        var collectionExpression = await CreateCollectionExpressionAsync(
            newDocument,
            dummyObjectCreation,
            preMatches: [],
            analysisResult.Matches.SelectAsArray(m => new CollectionExpressionMatch<SyntaxNode>(m.StatementOrExpression, m.UseSpread)),
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
            => new(analysisResult.DiagnosticLocation,
                   root.GetCurrentNode(analysisResult.LocalDeclarationStatement)!,
                   root.GetCurrentNode(analysisResult.CreationExpression)!,
                   analysisResult.Matches.SelectAsArray(m => new Match(root.GetCurrentNode(m.StatementOrExpression)!, m.UseSpread)),
                   analysisResult.ChangesSemantics);

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

            nodesToTrack.Add(analysisResult.LocalDeclarationStatement);
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
    }
}
