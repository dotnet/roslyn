// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExpressionBodyForLambda), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class UseExpressionBodyForLambdaCodeFixProvider() : ForkingSyntaxEditorBasedCodeFixProvider<LambdaExpressionSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId];

    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var title = diagnostic.GetMessage();
        return (title, title);
    }

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        LambdaExpressionSyntax lambdaExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(
            lambdaExpression,
            UseExpressionBodyForLambdaCodeActionHelpers.Update(
                semanticModel, lambdaExpression, cancellationToken));
    }
}
