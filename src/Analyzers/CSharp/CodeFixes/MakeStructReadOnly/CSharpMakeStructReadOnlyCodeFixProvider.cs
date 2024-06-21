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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructReadOnly;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeStructReadOnly), Shared]
internal sealed class CSharpMakeStructReadOnlyCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMakeStructReadOnlyCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Make_struct_readonly, nameof(CSharpAnalyzersResources.Make_struct_readonly));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        var typeDeclarations = diagnostics.Select(d => d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken));

        // process from lower to higher, that way we will fixup a nested struct first before fixing the outer struct.
        foreach (var typeDeclaration in typeDeclarations.OrderByDescending(t => t.SpanStart))
        {
            editor.ReplaceNode(
                typeDeclaration,
                (current, generator) => generator.WithModifiers(current, generator.GetModifiers(current).WithIsReadOnly(true)));
        }

        return Task.CompletedTask;
    }
}
