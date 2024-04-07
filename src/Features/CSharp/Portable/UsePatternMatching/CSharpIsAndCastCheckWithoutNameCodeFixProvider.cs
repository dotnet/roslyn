// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UsePatternMatchingIsAndCastCheckWithoutName), Shared]
internal partial class CSharpIsAndCastCheckWithoutNameCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpIsAndCastCheckWithoutNameCodeFixProvider()
        : base(supportsFixAll: false)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.InlineIsTypeWithoutNameCheckDiagnosticsId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(
            CodeAction.Create(
                CSharpAnalyzersResources.Use_pattern_matching,
                GetDocumentUpdater(context),
                nameof(CSharpAnalyzersResources.Use_pattern_matching),
                CodeActionPriority.Low),
            context.Diagnostics);
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        Debug.Assert(diagnostics.Length == 1);
        var location = diagnostics[0].Location;
        var isExpression = (BinaryExpressionSyntax)location.FindNode(
            getInnermostNodeForTie: true, cancellationToken: cancellationToken);

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var expressionTypeOpt = semanticModel.Compilation.ExpressionOfTType();

        var (matches, localName) = CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzer.AnalyzeExpression(
            semanticModel, isExpression, expressionTypeOpt, cancellationToken);

        var updatedSemanticModel = CSharpIsAndCastCheckWithoutNameDiagnosticAnalyzer.ReplaceMatches(
            semanticModel, isExpression, localName, matches, cancellationToken);

        var updatedRoot = updatedSemanticModel.SyntaxTree.GetRoot(cancellationToken);
        editor.ReplaceNode(editor.OriginalRoot, updatedRoot);
    }
}
