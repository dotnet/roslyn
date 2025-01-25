// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementNotImplementedException), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpImplementNotImplementedExceptionCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.CopilotImplementNotImplementedExceptionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Implement_with_Copilot, nameof(CSharpAnalyzersResources.Implement_with_Copilot));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
            await FixOneAsync(editor, diagnostic, cancellationToken).ConfigureAwait(false);
    }

    private static async Task FixOneAsync(
        SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // Find the throw statement node
        var throwExpressionOrStatement = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

        //// Create a replacement node (a simple comment in this case)
        //var commentTrivia = SyntaxFactory.Comment("// TODO: Implement this method");
        //var commentStatement = SyntaxFactory.ExpressionStatement(SyntaxFactory.IdentifierName(commentTrivia.ToString()));

        //// Replace the throw statement with the comment
        //editor.ReplaceNode(throwExpressionOrStatement, commentStatement);
    }
}
