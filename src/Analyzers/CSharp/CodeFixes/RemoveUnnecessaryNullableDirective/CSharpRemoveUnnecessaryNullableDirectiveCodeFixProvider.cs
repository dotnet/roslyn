// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryNullableDirective;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableDirective)]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryNullableDirectiveCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [
            IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId,
            IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId,
        ];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId)
                RegisterCodeFix(context, CSharpAnalyzersResources.Remove_redundant_nullable_directive, nameof(CSharpAnalyzersResources.Remove_redundant_nullable_directive), diagnostic);
            else
                RegisterCodeFix(context, CSharpAnalyzersResources.Remove_unnecessary_nullable_directive, nameof(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive), diagnostic);
        }

        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            // Remove the directive. Use SyntaxRemoveOptions.AddElasticMarker so the code action post-processing
            // steps know to remove extraneous blank lines appearing after the removal.
            var nullableDirective = diagnostic.Location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken);
            editor.RemoveNode(nullableDirective, SyntaxRemoveOptions.AddElasticMarker);
        }

        return Task.CompletedTask;
    }
}
