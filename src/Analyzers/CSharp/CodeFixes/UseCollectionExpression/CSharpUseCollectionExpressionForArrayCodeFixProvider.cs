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
internal partial class CSharpUseCollectionExpressionForArrayCodeFixProvider
    : ForkingSyntaxEditorBasedCodeFixProvider<ExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForArrayCodeFixProvider()
        : base(CSharpCodeFixesResources.Use_collection_expression,
               IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId);

    protected sealed override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        ExpressionSyntax arrayCreationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var services = document.Project.Solution.Services;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var originalRoot = editor.OriginalRoot;

        if (arrayCreationExpression
                is not ArrayCreationExpressionSyntax
                and not ImplicitArrayCreationExpressionSyntax
                and not InitializerExpressionSyntax)
        {
            return;
        }

        if (arrayCreationExpression is InitializerExpressionSyntax initializer)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(
                initializer,
                ConvertInitializerToCollectionExpression(
                    initializer,
                    IsOnSingleLine(text, initializer)));
        }
        else
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var matches = GetMatches(semanticModel, arrayCreationExpression);
            if (matches.IsDefault)
                return;

            var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
                document,
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

            editor.ReplaceNode(arrayCreationExpression, collectionExpression);
            foreach (var match in matches)
                editor.RemoveNode(match.Node);
        }

        return;

        static bool IsOnSingleLine(SourceText sourceText, SyntaxNode node)
            => sourceText.AreOnSameLine(node.GetFirstToken(), node.GetLastToken());

        ImmutableArray<CollectionExpressionMatch<StatementSyntax>> GetMatches(SemanticModel semanticModel, ExpressionSyntax expression)
            => expression switch
            {
                // if we have `new[] { ... }` we have no subsequent matches to add to the collection. All values come
                // from within the initializer.
                ImplicitArrayCreationExpressionSyntax
                    => ImmutableArray<CollectionExpressionMatch<StatementSyntax>>.Empty,

                // we have `stackalloc T[...] ...;` defer to analyzer to find the items that follow that may need to
                // be added to the collection expression.
                ArrayCreationExpressionSyntax arrayCreation
                    => CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer.TryGetMatches(semanticModel, arrayCreation, cancellationToken),

                // We validated this is unreachable in the caller.
                _ => throw ExceptionUtilities.Unreachable(),
            };
    }
}
