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
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;
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
        RegisterCodeFix(context, CSharpCodeFixesResources.Use_collection_expression, nameof(CSharpCodeFixesResources.Use_collection_expression));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            var expression = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (expression is InitializerExpressionSyntax initializer)
            {
                RewriteInitializerExpression(initializer);
            }
            else if (expression is ArrayCreationExpressionSyntax arrayCreation)
            {
                RewriteArrayCreationExpression(arrayCreation);
            }
            else if (expression is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
            {
                RewriteImplicitArrayCreationExpression(implicitArrayCreation);
            }
        }

        return;

        static bool IsOnSingleLine(SourceText sourceText, SyntaxNode node)
            => sourceText.AreOnSameLine(node.GetFirstToken(), node.GetLastToken());

        void RewriteInitializerExpression(InitializerExpressionSyntax initializer)
        {
            editor.ReplaceNode(
                initializer,
                (current, _) => ConvertInitializerToCollectionExpression(
                    (InitializerExpressionSyntax)current,
                    IsOnSingleLine(sourceText, initializer)));
        }

        void RewriteArrayCreationExpression(ArrayCreationExpressionSyntax arrayCreation)
        {
            Contract.ThrowIfNull(arrayCreation.Initializer);

            editor.ReplaceNode(
                arrayCreation,
                (current, _) =>
                {
                    var currentArrayCreation = (ArrayCreationExpressionSyntax)current;
                    Contract.ThrowIfNull(currentArrayCreation.Initializer);

                    var isOnSingleLine = IsOnSingleLine(sourceText, arrayCreation.Initializer);
                    var collectionExpression = ConvertInitializerToCollectionExpression(
                        currentArrayCreation.Initializer, isOnSingleLine);

                    return ReplaceWithCollectionExpression(
                        sourceText, arrayCreation.Initializer, collectionExpression, isOnSingleLine);
                });
        }

        void RewriteImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
        {
            Contract.ThrowIfNull(implicitArrayCreation.Initializer);

            editor.ReplaceNode(
                implicitArrayCreation,
                (current, _) =>
                {
                    var currentArrayCreation = (ImplicitArrayCreationExpressionSyntax)current;
                    Contract.ThrowIfNull(currentArrayCreation.Initializer);

                    var isOnSingleLine = IsOnSingleLine(sourceText, implicitArrayCreation);
                    var collectionExpression = ConvertInitializerToCollectionExpression(
                        currentArrayCreation.Initializer, isOnSingleLine);

                    return ReplaceWithCollectionExpression(
                        sourceText, implicitArrayCreation.Initializer, collectionExpression, isOnSingleLine);
                });
        }
    }
}
