// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseNotPattern), Shared]
internal sealed class CSharpUseNotPatternCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseNotPatternCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseNotPatternDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_pattern_matching, nameof(CSharpAnalyzersResources.Use_pattern_matching));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProcessDiagnostic(semanticModel, editor, diagnostic, cancellationToken);
        }
    }

    private static void ProcessDiagnostic(
        SemanticModel semanticModel,
        SyntaxEditor editor,
        Diagnostic diagnostic,
        CancellationToken cancellationToken)
    {
        var notExpressionLocation = diagnostic.AdditionalLocations[0];

        var notExpression = (PrefixUnaryExpressionSyntax)notExpressionLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);
        var parenthesizedExpression = (ParenthesizedExpressionSyntax)notExpression.Operand;

        var negated = editor.Generator.Negate(
            CSharpSyntaxGeneratorInternal.Instance,
            parenthesizedExpression.Expression,
            semanticModel,
            cancellationToken);

        editor.ReplaceNode(
            notExpression,
            negated.WithPrependedLeadingTrivia(notExpression.GetLeadingTrivia())
                   .WithAppendedTrailingTrivia(notExpression.GetTrailingTrivia()));
    }
}
