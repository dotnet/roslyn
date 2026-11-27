// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class WrapDocumentationInSummaryCodeActionProvider : IRazorCodeActionProvider
{
    public Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
    {
        if (context.Request.Context.Diagnostics is not { Length: > 0 } diagnostics)
        {
            return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
        }

        var syntaxTree = context.CodeDocument.GetRequiredSyntaxTree();
        var selection = TextSpan.FromBounds(context.StartAbsoluteIndex, context.EndAbsoluteIndex);
        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (diagnostic.Code is not { } code ||
                !code.TryGetSecond(out var diagnosticId) ||
                diagnosticId != RazorDiagnosticFactory.Parsing_DocumentationShouldStartWithTag.Id)
            {
                continue;
            }

            // RZ1049 marks the first non-whitespace character. The whole body, including any inline
            // XML, is stored in a CSharpStatementLiteralSyntax.
            var span = context.SourceText.GetTextSpan(diagnostic.Range);
            if (!span.IntersectsWith(selection) ||
                syntaxTree.Root.FindToken(span.Start).Parent is not CSharpStatementLiteralSyntax content)
            {
                continue;
            }

            // Keep outer whitespace outside the tags, and use the parsed body's end so
            // missing-brace recovery still leaves following code alone.
            var end = content.SpanStart + content.GetContent().AsSpan().TrimEnd().Length;

            var workspaceEdit = new WorkspaceEdit
            {
                DocumentChanges = new TextDocumentEdit[]
                {
                    new()
                    {
                        TextDocument = new OptionalVersionedTextDocumentIdentifier
                        {
                            DocumentUri = context.Request.TextDocument.DocumentUri,
                        },
                        Edits =
                        [
                            LspFactory.CreateTextEdit(context.SourceText.GetRange(new TextSpan(span.Start, 0)), "<summary>"),
                            LspFactory.CreateTextEdit(context.SourceText.GetRange(new TextSpan(end, 0)), "</summary>"),
                        ],
                    },
                },
            };

            var action = RazorCodeActionFactory.CreateWrapDocumentationInSummary(workspaceEdit);
            return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([action]);
        }

        return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
    }
}
