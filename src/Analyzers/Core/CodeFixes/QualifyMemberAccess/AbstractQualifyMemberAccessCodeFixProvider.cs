// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.QualifyMemberAccess;

internal abstract class AbstractQualifyMemberAccessCodeFixprovider<TSimpleNameSyntax, TInvocationSyntax>
    : SyntaxEditorBasedCodeFixProvider
    where TSimpleNameSyntax : SyntaxNode
    where TInvocationSyntax : SyntaxNode
{
    protected abstract string GetTitle();
    protected abstract TSimpleNameSyntax? GetNode(Diagnostic diagnostic, CancellationToken cancellationToken);

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.AddThisOrMeQualificationDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var title = GetTitle();
        RegisterCodeFix(context, title, title);
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

        foreach (var diagnostic in diagnostics)
        {
            var node = GetNode(diagnostic, cancellationToken);
            if (node != null)
            {
                var qualifiedAccess =
                    generator.MemberAccessExpression(
                        generator.ThisExpression(),
                        node.WithLeadingTrivia())
                    .WithLeadingTrivia(node.GetLeadingTrivia());

                editor.ReplaceNode(node, qualifiedAccess);
            }
        }
    }
}
