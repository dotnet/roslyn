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
using Microsoft.CodeAnalysis.CSharp.Analyzers.HiddenExplicitCast;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ForEachCast;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HiddenExplicitCast;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.HiddenExplicitCast), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpHiddenExplicitCastCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.HiddenExplicitCastDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Add_explicit_cast, nameof(AnalyzersResources.Add_explicit_cast));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            if (diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken) is not CastExpressionSyntax castExpression)
                continue;

            var typeToInsert = SyntaxFactory.ParseTypeName(diagnostic.Properties[CSharpHiddenExplicitCastDiagnosticAnalyzer.Type]!);
            editor.ReplaceNode(
                castExpression,
                (current, g) =>
                {
                    var currentCast = (CastExpressionSyntax)current;
                    return currentCast.WithExpression((ExpressionSyntax)g.CastExpression(typeToInsert, currentCast.Expression));
                });
        }

        return Task.CompletedTask;
    }
}
