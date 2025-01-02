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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.ConsecutiveStatementPlacement), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ConsecutiveStatementPlacementCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.ConsecutiveStatementPlacementDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(CodeAction.Create(
            CodeFixesResources.Add_blank_line_after_block,
            c => UpdateDocumentAsync(document, diagnostic, c),
            nameof(CodeFixesResources.Add_blank_line_after_block)),
            context.Diagnostics);
        return Task.CompletedTask;
    }

    private static Task<Document> UpdateDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        => FixAllAsync(document, [diagnostic], cancellationToken);

    public static async Task<Document> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var options = await document.GetLineFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var generator = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
        var endOfLineTrivia = generator.EndOfLine(options.NewLine);

        var nextTokens = diagnostics.Select(d => d.AdditionalLocations[0].FindToken(cancellationToken));
        var newRoot = root.ReplaceTokens(
            nextTokens,
            (original, current) => current.WithLeadingTrivia(current.LeadingTrivia.Insert(0, endOfLineTrivia)));

        return document.WithSyntaxRoot(newRoot);
    }

    public override FixAllProvider GetFixAllProvider()
        => FixAllProvider.Create(async (context, document, diagnostics) => await FixAllAsync(document, diagnostics, context.CancellationToken).ConfigureAwait(false));
}
