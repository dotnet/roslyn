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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyPropertyPattern;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyPropertyPattern), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSimplifyPropertyPatternCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.SimplifyPropertyPatternDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Simplify_property_pattern, nameof(CSharpAnalyzersResources.Simplify_property_pattern));
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        // Process subpatterns in reverse order so we rewrite from inside-to-outside with nested
        // patterns.
        var subpatterns = diagnostics.Select(d => (SubpatternSyntax)d.AdditionalLocations[0].FindNode(cancellationToken))
                                     .OrderByDescending(s => s.SpanStart)
                                     .ToImmutableArray();

        foreach (var subpattern in subpatterns)
        {
            editor.ReplaceNode(
                subpattern,
                (current, _) =>
                {
                    var currentSubpattern = (SubpatternSyntax)current;
                    var simplified = TrySimplify(currentSubpattern);
                    return simplified ?? currentSubpattern;
                });
        }
    }

    private static SubpatternSyntax? TrySimplify(SubpatternSyntax currentSubpattern)
    {
        if (!SimplifyPropertyPatternHelpers.IsSimplifiable(currentSubpattern, out var innerSubpattern, out var outerExpressionColon))
            return null;

        // attempt to simplify the inner pattern we're pointing at as well (that way if the user
        // invokes the fix on a top level property, we collapse as far inwards as possible).
        innerSubpattern = TrySimplify(innerSubpattern) ?? innerSubpattern;

        var innerExpressionColon = innerSubpattern.ExpressionColon;

        if (!SimplifyPropertyPatternHelpers.IsMergable(outerExpressionColon.Expression) ||
            !SimplifyPropertyPatternHelpers.IsMergable(innerExpressionColon?.Expression))
        {
            return null;
        }

        var merged = Merge(outerExpressionColon, innerExpressionColon);
        if (merged == null)
            return null;

        return currentSubpattern.WithExpressionColon(merged)
                                .WithPattern(innerSubpattern.Pattern)
                                .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static BaseExpressionColonSyntax? Merge(BaseExpressionColonSyntax outerExpressionColon, BaseExpressionColonSyntax innerExpressionColon)
    {
        var merged = Merge(outerExpressionColon.Expression, innerExpressionColon.Expression);
        if (merged == null)
            return null;

        return outerExpressionColon.WithExpression(merged);
    }

    private static MemberAccessExpressionSyntax? Merge(ExpressionSyntax? outerExpression, ExpressionSyntax? innerExpression)
    {
        if (outerExpression == null || innerExpression == null)
            return null;

        // if the inner name is simple (i.e. just 'X') we can trivially form the final member
        // access by joining the two names together.
        if (innerExpression is SimpleNameSyntax innerName)
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, outerExpression, innerName);

        if (innerExpression is not MemberAccessExpressionSyntax innerMemberAccess)
            return null;

        // otherwise, attempt to decompose the inner expression, joining that with the outer until we get
        // the result.
        return Merge(Merge(outerExpression, innerMemberAccess.Expression), innerMemberAccess.Name);
    }
}
