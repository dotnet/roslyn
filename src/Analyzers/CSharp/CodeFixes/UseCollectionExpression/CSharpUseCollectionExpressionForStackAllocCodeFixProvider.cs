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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForStackAlloc), Shared]
internal partial class CSharpUseCollectionExpressionForStackAllocCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForStackAllocCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId);

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpCodeFixesResources.Use_collection_expression, IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId);
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

        var stackallocExpressions = new Stack<ExpressionSyntax>();
        foreach (var diagnostic in diagnostics)
        {
            var expression = (ExpressionSyntax)originalRoot.FindNode(
                diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            stackallocExpressions.Push(expression);
        }

        // We're going to be continually editing this tree.  Track all the nodes we
        // care about so we can find them across each edit.
        var semanticDocument = await SemanticDocument.CreateAsync(
            document.WithSyntaxRoot(originalRoot.TrackNodes(stackallocExpressions)),
            cancellationToken).ConfigureAwait(false);

        while (stackallocExpressions.Count > 0)
        {
            var stackAllocExpression = semanticDocument.Root.GetCurrentNodes(stackallocExpressions.Pop()).Single();
            if (stackAllocExpression is not StackAllocArrayCreationExpressionSyntax and not ImplicitStackAllocArrayCreationExpressionSyntax)
                continue;

            var matches = GetMatches(semanticDocument, stackAllocExpression);
            if (matches.IsDefault)
                continue;

            var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
                semanticDocument.Document,
                fallbackOptions,
                stackAllocExpression,
                matches,
                static e => e switch
                {
                    StackAllocArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
                    ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.Initializer,
                    _ => throw ExceptionUtilities.Unreachable(),
                },
                static (e, i) => e switch
                {
                    StackAllocArrayCreationExpressionSyntax arrayCreation => arrayCreation.WithInitializer(i),
                    ImplicitStackAllocArrayCreationExpressionSyntax implicitArrayCreation => implicitArrayCreation.WithInitializer(i),
                    _ => throw ExceptionUtilities.Unreachable(),
                },
                cancellationToken).ConfigureAwait(false);

            var subEditor = new SyntaxEditor(semanticDocument.Root, services);
            subEditor.ReplaceNode(stackAllocExpression, collectionExpression);

            foreach (var match in matches)
                subEditor.RemoveNode(match.Statement);

            semanticDocument = await semanticDocument.WithSyntaxRootAsync(
                subEditor.GetChangedRoot(), cancellationToken).ConfigureAwait(false);
        }

        editor.ReplaceNode(originalRoot, semanticDocument.Root);
        return;

        ImmutableArray<CollectionExpressionMatch> GetMatches(SemanticDocument document, ExpressionSyntax stackAllocExpression)
        {
            switch (stackAllocExpression)
            {
                case ImplicitStackAllocArrayCreationExpressionSyntax:
                    // if we have `stackalloc[] { ... }` we have no subsequent matches to add to the collection. All values come
                    // from within the initializer.
                    return ImmutableArray<CollectionExpressionMatch>.Empty;

                case StackAllocArrayCreationExpressionSyntax arrayCreation:
                    // we have `stackalloc T[...] ...;` defer to analyzer to find the items that follow that may need to
                    // be added to the collection expression.
                    return CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer.TryGetMatches(
                        document.SemanticModel, arrayCreation, cancellationToken);

                default:
                    // We validated this is unreachable in the caller.
                    throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
