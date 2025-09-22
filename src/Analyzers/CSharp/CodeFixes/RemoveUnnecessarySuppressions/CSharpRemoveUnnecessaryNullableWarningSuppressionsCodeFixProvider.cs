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
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableWarningSuppressions), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider()
    : ForkingSyntaxEditorBasedCodeFixProvider<PostfixUnaryExpressionSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveUnnecessaryNullableWarningSuppression];

    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
        => (AnalyzersResources.Remove_unnecessary_suppression, nameof(AnalyzersResources.Remove_unnecessary_suppression));

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        PostfixUnaryExpressionSyntax diagnosticNode,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (UnnecessaryNullableWarningSuppressionsUtilities.IsUnnecessary(semanticModel, diagnosticNode, cancellationToken))
        {
            editor.ReplaceNode(diagnosticNode, diagnosticNode.Operand.WithTriviaFrom(diagnosticNode));
        }
    }
}
