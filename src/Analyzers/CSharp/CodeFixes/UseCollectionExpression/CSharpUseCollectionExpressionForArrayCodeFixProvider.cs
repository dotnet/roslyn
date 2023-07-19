// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseConditionalExpression;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForArray), Shared]
internal partial class CSharpUseCollectionExpressionForArrayCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForArrayCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId);

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
            var expression = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (expression is InitializerExpressionSyntax initializerExpression)
            {
                RewriteInitializerExpression(initializerExpression);
            }
        }

        return;

        void RewriteInitializerExpression(InitializerExpressionSyntax initializer)
        {
            // if the initializer is already on multiple lines, keep it that way.  otherwise, squash from `{ 1, 2, 3 }` to `[1, 2, 3]`
            editor.ReplaceNode(
                initializer,
                (current, _) =>
                {
                    var currentInitializer = (InitializerExpressionSyntax)current;

                    var openBracket = Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(currentInitializer.OpenBraceToken);
                    var elements = currentInitializer.Expressions.GetWithSeparators().SelectAsArray(
                        i => i.IsToken ? i : ExpressionElement((ExpressionSyntax)i.AsNode()!));
                    var closeBracket = Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(currentInitializer.CloseBraceToken);

                    if (sourceText.AreOnSameLine(initializer.OpenBraceToken, initializer.CloseBraceToken))
                    {
                        // convert '{ ' to '['
                        if (openBracket.TrailingTrivia is [(kind: SyntaxKind.WhitespaceTrivia), ..])
                            openBracket = openBracket.WithTrailingTrivia(openBracket.TrailingTrivia.Skip(1));

                        if (elements is [.., var lastNodeOrToken] && lastNodeOrToken.GetTrailingTrivia() is [(kind: SyntaxKind.WhitespaceTrivia), ..])
                            elements = elements.Replace(lastNodeOrToken, lastNodeOrToken.WithTrailingTrivia(lastNodeOrToken.GetTrailingTrivia().Skip(1)));
                    }

                    return CollectionExpression(openBracket, SeparatedList<CollectionElementSyntax>(elements), closeBracket);
                });
        }
    }
}
