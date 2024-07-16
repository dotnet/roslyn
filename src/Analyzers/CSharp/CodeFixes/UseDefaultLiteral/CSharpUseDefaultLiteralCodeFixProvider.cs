// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseDefaultLiteral), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpUseDefaultLiteralCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseDefaultLiteralDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Simplify_default_expression, nameof(CSharpAnalyzersResources.Simplify_default_expression));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        // Fix-All for this feature is somewhat complicated.  Each time we fix one case, it
        // may make the next case unfixable.  For example:
        //
        //    'var v = x ? default(string) : default(string)'.
        //
        // Here, we can replace either of the default expressions, but not both. So we have 
        // to replace one at a time, and only actually replace if it's still safe to do so.

        var options = (CSharpAnalyzerOptionsProvider)await document.GetAnalyzerOptionsProviderAsync(cancellationToken).ConfigureAwait(false);
        var preferSimpleDefaultExpression = options.PreferSimpleDefaultExpression.Value;

        var originalRoot = editor.OriginalRoot;
        var parseOptions = (CSharpParseOptions)originalRoot.SyntaxTree.Options;

        var originalNodes = diagnostics.SelectAsArray(
            d => (DefaultExpressionSyntax)originalRoot.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true));

        await editor.ApplyExpressionLevelSemanticEditsAsync(
            document, originalNodes,
            (semanticModel, defaultExpression) => defaultExpression.CanReplaceWithDefaultLiteral(parseOptions, preferSimpleDefaultExpression, semanticModel, cancellationToken),
            (_, currentRoot, defaultExpression) => currentRoot.ReplaceNode(
                defaultExpression,
                SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression).WithTriviaFrom(defaultExpression)),
            cancellationToken).ConfigureAwait(false);
    }
}
