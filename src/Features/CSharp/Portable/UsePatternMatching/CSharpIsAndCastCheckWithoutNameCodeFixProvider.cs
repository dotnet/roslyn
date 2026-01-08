// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePatternMatchingIsAndCastCheckWithoutName), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpIsAndCastCheckWithoutNameCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider(supportsFixAll: false)
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.InlineIsTypeWithoutNameCheckDiagnosticsId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_pattern_matching, nameof(CSharpAnalyzersResources.Use_pattern_matching), CodeActionPriority.Low);
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        Debug.Assert(diagnostics.Length == 1);
        var location = diagnostics[0].Location;
        var isExpression = (BinaryExpressionSyntax)location.FindNode(
            getInnermostNodeForTie: true, cancellationToken: cancellationToken);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var expressionType = semanticModel.Compilation.ExpressionOfTType();

        using var _ = PooledHashSet<CastExpressionSyntax>.GetInstance(out var matches);
        var localName = CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzer.AnalyzeExpression(
            semanticModel, isExpression, expressionType, matches, cancellationToken);
        if (localName is null || matches.Count == 0)
            return;

        var updatedSemanticModel = CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzer.ReplaceMatches(
            semanticModel, isExpression, localName, matches, cancellationToken);

        var updatedRoot = updatedSemanticModel.SyntaxTree.GetRoot(cancellationToken);
        editor.ReplaceNode(editor.OriginalRoot, updatedRoot);
    }
}
