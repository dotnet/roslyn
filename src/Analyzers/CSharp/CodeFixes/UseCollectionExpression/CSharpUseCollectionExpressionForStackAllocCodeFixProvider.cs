﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForStackAlloc), Shared]
internal partial class CSharpUseCollectionExpressionForStackAllocCodeFixProvider
    : ForkingSyntaxEditorBasedCodeFixProvider<ExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForStackAllocCodeFixProvider()
        : base(CSharpCodeFixesResources.Use_collection_expression,
               IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId);

    protected sealed override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        ExpressionSyntax stackAllocExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        if (stackAllocExpression is not StackAllocArrayCreationExpressionSyntax and not ImplicitStackAllocArrayCreationExpressionSyntax)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var matches = GetMatches();
        if (matches.IsDefault)
            return;

        var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
            document,
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

        editor.ReplaceNode(stackAllocExpression, collectionExpression);

        foreach (var match in matches)
            editor.RemoveNode(match.Node);

        return;

        ImmutableArray<CollectionExpressionMatch<StatementSyntax>> GetMatches()
            => stackAllocExpression switch
            {
                // if we have `stackalloc[] { ... }` we have no subsequent matches to add to the collection. All values come
                // from within the initializer.
                ImplicitStackAllocArrayCreationExpressionSyntax
                    => ImmutableArray<CollectionExpressionMatch<StatementSyntax>>.Empty,

                // we have `stackalloc T[...] ...;` defer to analyzer to find the items that follow that may need to
                // be added to the collection expression.
                StackAllocArrayCreationExpressionSyntax arrayCreation
                    => CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer.TryGetMatches(semanticModel, arrayCreation, cancellationToken),

                // We validated this is unreachable in the caller.
                _ => throw ExceptionUtilities.Unreachable(),
            };
    }
}
