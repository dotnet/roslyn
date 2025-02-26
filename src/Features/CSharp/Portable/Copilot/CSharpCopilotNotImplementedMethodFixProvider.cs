// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        if (document.GetLanguageService<ICopilotOptionsService>() is not { } optionsService ||
                 await optionsService.IsImplementNotImplementedExceptionEnabledAsync().ConfigureAwait(false) is false)
        {
            return;
        }

        if (document.GetLanguageService<ICopilotCodeAnalysisService>() is not { } copilotService ||
            await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false) is false)
        {
            return;
        }

        // Find the throw statement or throw expression node
        var throwNode = context.Diagnostics[0].AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

        // Preliminary analysis before registering fix
        var memberDeclaration = throwNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDeclaration is BasePropertyDeclarationSyntax || memberDeclaration is BaseMethodDeclarationSyntax)
        {
            var x = DualChangeAction.New(CSharpAnalyzersResources.Implement_with_Copilot,
            // for the non preview
            async (_, cancellationToken) => await GetDocumentUpdater(context, null)(cancellationToken).ConfigureAwait(false),
            // no-op for the preview
            (_, _) => Task.FromResult(context.Document),
            nameof(CSharpAnalyzersResources.Implement_with_Copilot));

            context.RegisterCodeFix(x, context.Diagnostics);
        }
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
        var throwNode = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

        // Analyze document
        var analysisRecord = await DocumentAnalyzer.AnalyzeDocumentAsync(document, throwNode, cancellationToken).ConfigureAwait(false);
        if (analysisRecord == null)
        {
            return;
        }

        // Give the analysis as text to copilot and receive some answer
        var copilotService = document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();
        var suggestedCodeBlock = await CodeProvider.SuggestCodeBlockAsync(copilotService, document, throwNode.Span, analysisRecord, cancellationToken).ConfigureAwait(false);

        // Generate code
        CodeGenerator.GenerateCode(editor, throwNode, suggestedCodeBlock);
    }
}
