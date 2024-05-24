// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseImplicitObjectCreation;

using static SyntaxFactory;
using static CSharpUseImplicitObjectCreationDiagnosticAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseImplicitObjectCreation), Shared]
internal class CSharpUseImplicitObjectCreationCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseImplicitObjectCreationCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseImplicitObjectCreationDiagnosticId];

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.IsSuppressed;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_new, nameof(CSharpAnalyzersResources.Use_new));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        // process from inside->out so that outer rewrites see the effects of inner changes.
        var nodes = diagnostics
            .OrderBy(d => d.Location.SourceSpan.End)
            .SelectAsArray(d => (ObjectCreationExpressionSyntax)d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken));

#if CODE_STYLE
        var options = CSharpSimplifierOptions.Default;
#else
        var options = (CSharpSimplifierOptions)await document.GetSimplifierOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
#endif

        // Bulk apply these, except at the expression level.  One fix at the expression level may prevent another fix
        // from being valid.  For example: `new List<C> { new C() }`.  If we apply the fix to the outer `List<C>`, we
        // should not apply it to the inner `new C()` as the inner creation will no longer be apparent once the outer
        // type goes away.
        await editor.ApplyExpressionLevelSemanticEditsAsync(
            document,
            nodes,
            (semanticModel, node) => Analyze(semanticModel, options, node, cancellationToken),
            (semanticModel, root, node) => FixOne(root, node),
            cancellationToken).ConfigureAwait(false);
    }

    private static SyntaxNode FixOne(SyntaxNode root, ObjectCreationExpressionSyntax objectCreation)
    {
        var implicitObject = ImplicitObjectCreationExpression(
            WithoutTrailingWhitespace(objectCreation.NewKeyword),
            objectCreation.ArgumentList ?? ArgumentList(),
            objectCreation.Initializer);
        return root.ReplaceNode(objectCreation, implicitObject);
    }

    private static SyntaxToken WithoutTrailingWhitespace(SyntaxToken newKeyword)
        => newKeyword.TrailingTrivia.All(t => t.IsWhitespace())
            ? newKeyword.WithoutTrailingTrivia()
            : newKeyword;
}
