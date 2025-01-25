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
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseImplicitlyTypedLambdaExpression;

using static CSharpUseImplicitlyTypedLambdaExpressionDiagnosticAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseImplicitlyTypedLambdaExpression), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUseImplicitlyTypedLambdaExpressionCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseImplicitlyTypedLambdaExpressionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_implicitly_typed_lambda, nameof(CSharpAnalyzersResources.Use_implicitly_typed_lambda));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        // process from inside->out so that outer rewrites see the effects of inner changes.
        var nodes = diagnostics
            .OrderBy(d => d.Location.SourceSpan.End)
            .SelectAsArray(d => (ParenthesizedLambdaExpressionSyntax)d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken));

        var options = (CSharpSimplifierOptions)await document.GetSimplifierOptionsAsync(
            CSharpSimplification.Instance, cancellationToken).ConfigureAwait(false);

        // Bulk apply these, except at the expression level.  One fix at the expression level may prevent another fix
        // from being valid.
        await editor.ApplyExpressionLevelSemanticEditsAsync(
            document,
            nodes,
            (semanticModel, node) => Analyze(semanticModel, node, cancellationToken),
            (semanticModel, root, node) => FixOne(root, node),
            cancellationToken).ConfigureAwait(false);
    }

    private static SyntaxNode FixOne(SyntaxNode root, ParenthesizedLambdaExpressionSyntax explicitLambda)
        => root.ReplaceNode(explicitLambda, ConvertToImplicitlyTypedLambda(explicitLambda));
}
