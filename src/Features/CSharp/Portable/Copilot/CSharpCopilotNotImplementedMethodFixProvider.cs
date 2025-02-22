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
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ImplementNotImplementedException), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpCopilotNotImplementedMethodFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.CopilotImplementNotImplementedExceptionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var x = DualChangeAction.New(CSharpAnalyzersResources.Implement_with_Copilot,
            // for the non preview
            (_, c) => GetDocumentUpdater(context, null)(c),
            // no-op for the preview
            (_, _) => Task.FromResult(context.Document),
            nameof(CSharpAnalyzersResources.Implement_with_Copilot));

        context.RegisterCodeFix(x, context.Diagnostics);
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
            await FixOneAsync(editor, document, diagnostic, cancellationToken).ConfigureAwait(false);
    }

    private static async Task FixOneAsync(
        SyntaxEditor editor, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        // Find the throw statement node
        var throwStatement = diagnostic.AdditionalLocations[0]
            .FindNode(getInnermostNodeForTie: true, cancellationToken).AncestorsAndSelf().OfType<ThrowStatementSyntax>().FirstOrDefault();

        if (throwStatement == null)
        {
            return;
        }

        // Analyze document
        var analysisRecord = await DocumentAnalyzer.AnalyzeDocumentAsync(document, throwStatement, cancellationToken).ConfigureAwait(false);
        if (analysisRecord == null)
        {
            return;
        }

        // Give the analysis as text to copilot and receive some answer
        var copilotService = document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();
        var copilotSuggestedCodeBlock = await CopilotCodeProvider.GetCopilotSuggestedCodeBlockAsync(copilotService, analysisRecord, cancellationToken).ConfigureAwait(false);

        // Generate code
        CodeGenerator.GenerateCode(editor, throwStatement, copilotSuggestedCodeBlock);
    }
}
