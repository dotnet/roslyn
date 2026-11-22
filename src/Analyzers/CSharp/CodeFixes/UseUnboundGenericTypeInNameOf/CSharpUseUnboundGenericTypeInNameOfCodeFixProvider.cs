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

namespace Microsoft.CodeAnalysis.CSharp.UseUnboundGenericTypeInNameOf;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseUnboundGenericTypeInNameOf), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpUseUnboundGenericTypeInNameOfCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseUnboundGenericTypeInNameOfDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Use_unbound_generic_type, nameof(CSharpAnalyzersResources.Use_unbound_generic_type));
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
            FixOne(editor, diagnostic, cancellationToken);
    }

    private static void FixOne(
        SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var nameofInvocation = (InvocationExpressionSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
        if (!nameofInvocation.IsNameOfInvocation())
            return;

        editor.ReplaceNode(
            nameofInvocation, CSharpUseUnboundGenericTypeInNameOfDiagnosticAnalyzer.ConvertToUnboundGenericNameof(nameofInvocation));
    }
}
