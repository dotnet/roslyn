// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableWarningSuppressions), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveUnnecessaryNullableWarningSuppression];

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Remove_unnecessary_suppression, nameof(AnalyzersResources.Remove_unnecessary_suppression));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        using var _ = PooledHashSet<PostfixUnaryExpressionSyntax>.GetInstance(out var processedOuterNodes);

        // Process the nodes from outside in.  If we remove the outer suppression, then don't process any inner nodes
        // inside of that.  Those suppressions may now be necessary due to the outer going away.  this way we push
        // suppressions to the narrowest possible scope when there are multiple.
        foreach (var diagnostic in diagnostics.OrderBy(d => d.AdditionalLocations[0].SourceSpan.Start))
        {
            var node = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            if (node is not PostfixUnaryExpressionSyntax postfixUnary)
                continue;

            // Skip inner nodes if we already processed an outer node.
            if (node.Ancestors().OfType<PostfixUnaryExpressionSyntax>().Any(processedOuterNodes.Contains))
                continue;

            editor.ReplaceNode(
                postfixUnary,
                postfixUnary.Operand.WithTriviaFrom(postfixUnary));
            processedOuterNodes.Add(postfixUnary);
        }

        return Task.CompletedTask;
    }
}
