// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForArray), Shared]
internal partial class CSharpUseCollectionExpressionForArrayCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForArrayCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId);

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpCodeFixesResources.Use_collection_expression, IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId);
        return Task.CompletedTask;
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var services = document.Project.Solution.Services;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var originalRoot = editor.OriginalRoot;

        var arrayExpressions = new Stack<ExpressionSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            var expression = (ExpressionSyntax)originalRoot.FindNode(
                diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            arrayExpressions.Push(expression);
        }

        // We're going to be continually editing this tree.  Track all the nodes we
        // care about so we can find them across each edit.
        var semanticDocument = await SemanticDocument.CreateAsync(
            document.WithSyntaxRoot(originalRoot.TrackNodes(arrayExpressions)),
            cancellationToken).ConfigureAwait(false);

        while (arrayExpressions.Count > 0)
        {
            var arrayCreationExpression = semanticDocument.Root.GetCurrentNodes(arrayExpressions.Pop()).Single();
            if (arrayCreationExpression
                    is not ArrayCreationExpressionSyntax
                    and not ImplicitArrayCreationExpressionSyntax
                    and not InitializerExpressionSyntax)
            {
                continue;
            }

            var subEditor = new SyntaxEditor(semanticDocument.Root, services);

            if (arrayCreationExpression is InitializerExpressionSyntax initializer)
            {
                subEditor.ReplaceNode(
                    initializer,
                    ConvertInitializerToCollectionExpression(
                        initializer,
                        IsOnSingleLine(semanticDocument.Text, initializer)));
            }
            else
            {
                var matches = GetMatches(semanticDocument, arrayCreationExpression);
                if (matches.IsDefault)
                    continue;

                var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
                    semanticDocument.Document,
                    fallbackOptions,
                    arrayCreationExpression,
                    matches,
                    static e => e switch
                    {
                        ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
                        ImplicitArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.Initializer,
                        _ => throw ExceptionUtilities.Unreachable(),
                    },
                    static (e, i) => e switch
                    {
                        ArrayCreationExpressionSyntax arrayCreation => arrayCreation.WithInitializer(i),
                        ImplicitArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.WithInitializer(i),
                        _ => throw ExceptionUtilities.Unreachable(),
                    },
                    cancellationToken).ConfigureAwait(false);

                subEditor.ReplaceNode(arrayCreationExpression, collectionExpression);
                foreach (var match in matches)
                    subEditor.RemoveNode(match.Statement);
            }

            semanticDocument = await semanticDocument.WithSyntaxRootAsync(
                subEditor.GetChangedRoot(), cancellationToken).ConfigureAwait(false);
        }

        editor.ReplaceNode(originalRoot, semanticDocument.Root);
        return;

        static bool IsOnSingleLine(SourceText sourceText, SyntaxNode node)
            => sourceText.AreOnSameLine(node.GetFirstToken(), node.GetLastToken());

        ImmutableArray<CollectionExpressionMatch> GetMatches(SemanticDocument document, ExpressionSyntax expression)
        {
            switch (expression)
            {
                case ImplicitArrayCreationExpressionSyntax:
                    // if we have `new[] { ... }` we have no subsequent matches to add to the collection. All values come
                    // from within the initializer.
                    return ImmutableArray<CollectionExpressionMatch>.Empty;

                case ArrayCreationExpressionSyntax arrayCreation:
                    // we have `stackalloc T[...] ...;` defer to analyzer to find the items that follow that may need to
                    // be added to the collection expression.
                    return CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer.TryGetMatches(
                        document.SemanticModel, arrayCreation, cancellationToken);

                default:
                    // We validated this is unreachable in the caller.
                    throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
