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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseUnboundGenericTypeInNameOf;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseUnboundGenericTypeInNameOf), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpUseUnboundGenericTypeInNameOfCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    private static readonly SyntaxNodeOrToken s_omittedArgument = (SyntaxNodeOrToken)OmittedTypeArgument();

    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseUnboundGenericTypeInNameOfDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_unbound_generic_type, nameof(CSharpAnalyzersResources.Use_unbound_generic_type));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
            FixOne(editor, diagnostic, cancellationToken);

        return Task.CompletedTask;
    }

    private static void FixOne(
        SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var nameofInvocation = (InvocationExpressionSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
        if (!nameofInvocation.IsNameOfInvocation())
            return;

        foreach (var typeArgumentList in nameofInvocation.DescendantNodes().OfType<TypeArgumentListSyntax>().OrderByDescending(t => t.SpanStart))
        {
            if (typeArgumentList.Arguments.Any(a => a.Kind() != SyntaxKind.OmittedTypeArgument))
            {
                var list = NodeOrTokenList(typeArgumentList.Arguments.GetWithSeparators().Select(
                    t => t.IsToken ? t.AsToken().WithoutTrivia() : s_omittedArgument));
                editor.ReplaceNode(typeArgumentList, typeArgumentList.WithArguments(SeparatedList<TypeSyntax>(list)));
            }
        }
    }
}
