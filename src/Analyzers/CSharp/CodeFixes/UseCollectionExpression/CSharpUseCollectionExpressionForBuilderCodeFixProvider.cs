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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForBuilder), Shared]
internal partial class CSharpUseCollectionExpressionForBuilderCodeFixProvider
    : ForkingSyntaxEditorBasedCodeFixProvider<InvocationExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForBuilderCodeFixProvider()
        : base(CSharpCodeFixesResources.Use_collection_expression,
               IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId);

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        InvocationExpressionSyntax invocationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var matchOpt = CSharpUseCollectionExpressionForBuilderDiagnosticAnalyzer.AnalyzeInvocation(
            semanticModel, invocationExpression, cancellationToken);
        if (matchOpt is not { } fullMatch)
            return;

        // We want to replace the final invocation (`builder.ToImmutable()`) with `new()`.  That way we can call into
        // the collection-rewriter to swap out that object-creation with the new collection-expression.

        // First, mark all the nodes we care about, so we can find them once we do the replacement with the
        // object-creation expression.
        var dummyObjectAnnotation = new SyntaxAnnotation();
        var newDocument = await CreateTrackedDocumentAsync(
            document, fullMatch, dummyObjectAnnotation, cancellationToken).ConfigureAwait(false);

        var root = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var dummyObjectCreation = (ImplicitObjectCreationExpressionSyntax)root.GetAnnotatedNodes(dummyObjectAnnotation).Single();

        // Move the original match over to this rewritten tree.
        fullMatch = TrackMatch(root, fullMatch);

        // Get the new collection expression.
        var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
            newDocument,
            fallbackOptions,
            dummyObjectCreation,
            fullMatch.Matches.SelectAsArray(m => new CollectionExpressionMatch<StatementSyntax>(m.Statement, m.UseSpread)),
            static o => o.Initializer,
            static (o, i) => o.WithInitializer(i),
            cancellationToken).ConfigureAwait(false);

        var subEditor = new SyntaxEditor(root, document.Project.Solution.Services);

        // Remove the actual declaration of the builder.
        subEditor.RemoveNode(fullMatch.LocalDeclarationStatement);

        // Remove all the nodes mutating the builder.
        foreach (var (statement, _) in fullMatch.Matches)
            subEditor.RemoveNode(statement);

        // Finally, replace the invocation where we convert the builder to a collection with the new collection expression.
        subEditor.ReplaceNode(dummyObjectCreation, collectionExpression);

        editor.ReplaceNode(editor.OriginalRoot, subEditor.GetChangedRoot());
    }

    private static CollectionBuilderMatch TrackMatch(SyntaxNode root, CollectionBuilderMatch fullMatch)
        => new(fullMatch.DiagnosticLocation,
               root.GetCurrentNode(fullMatch.LocalDeclarationStatement)!,
               root.GetCurrentNode(fullMatch.CreationExpression)!,
               fullMatch.Matches.SelectAsArray(m => new Match<StatementSyntax>(root.GetCurrentNode(m.Statement)!, m.UseSpread)));

    private static async Task<Document> CreateTrackedDocumentAsync(
        Document document,
        CollectionBuilderMatch fullMatch,
        SyntaxAnnotation annotation,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToTrack);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        nodesToTrack.Add(fullMatch.LocalDeclarationStatement);
        foreach (var (node, _) in fullMatch.Matches)
            nodesToTrack.Add(node);
        nodesToTrack.Add(fullMatch.CreationExpression);

        var newRoot = root.TrackNodes(nodesToTrack);
        var creationExpression = newRoot.GetCurrentNode(fullMatch.CreationExpression)!;

        var dummyObjectCreation = ImplicitObjectCreationExpression()
            .WithTriviaFrom(creationExpression)
            .WithAdditionalAnnotations(annotation);

        var newDocument = document.WithSyntaxRoot(newRoot.ReplaceNode(creationExpression, dummyObjectCreation));
        return newDocument;
    }
}
