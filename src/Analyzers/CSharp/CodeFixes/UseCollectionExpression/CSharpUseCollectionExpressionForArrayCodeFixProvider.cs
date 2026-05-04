// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionExpression;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForArray), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpUseCollectionExpressionForArrayCodeFixProvider()
    : AbstractUseCollectionExpressionCodeFixProvider<ExpressionSyntax>(
        CSharpCodeFixesResources.Use_collection_expression,
        IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId)
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId];

    protected sealed override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
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
            var expressionType = semanticModel.Compilation.ExpressionOfTType();
            var matches = GetMatches(semanticModel, arrayCreationExpression, expressionType);
            if (matches.IsDefault)
                return;

            var collectionExpression = await CSharpCollectionExpressionRewriter.CreateCollectionExpressionAsync(
                document,
                arrayCreationExpression,
                preMatches: [],
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

        ImmutableArray<CollectionMatch<StatementSyntax>> GetMatches(
            SemanticModel semanticModel, ExpressionSyntax expression, INamedTypeSymbol? expressionType)
            => expression switch
            {
                ImplicitArrayCreationExpressionSyntax arrayCreation
                    => CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer.TryGetMatches(
                        semanticModel, arrayCreation, CreateReplacementCollectionExpressionForAnalysis(arrayCreation.Initializer), expressionType, allowSemanticsChange: true, cancellationToken, out _),

                ArrayCreationExpressionSyntax arrayCreation
                    => CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer.TryGetMatches(
                        semanticModel, arrayCreation, CreateReplacementCollectionExpressionForAnalysis(arrayCreation.Initializer), expressionType, allowSemanticsChange: true, cancellationToken, out _),

                // We validated this is unreachable in the caller.
                _ => throw ExceptionUtilities.Unreachable(),
            };
    }
}
