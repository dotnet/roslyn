// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
internal sealed class UseExplicitExpressionCodeActionProvider : IRazorCodeActionProvider
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
                diagnosticId != RazorDiagnosticFactory.Parsing_DocumentationDirectiveWillChangeMeaning.Id)
            {
                continue;
            }

            // The diagnostic may cover just the leading identifier, not the whole expression.
            var span = context.SourceText.GetTextSpan(diagnostic.Range);
            if (!span.IntersectsWith(selection) ||
                syntaxTree.Root.FindToken(span.Start).Parent?.FirstAncestorOrSelf<CSharpImplicitExpressionSyntax>() is not { } expression)
            {
                continue;
            }

            // Wrap the whole expression, including calls and member access, but leave any following
            // text or braces outside the parentheses so they keep their existing meaning.
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
                            LspFactory.CreateTextEdit(context.SourceText.GetRange(new TextSpan(expression.Body.SpanStart, 0)), "("),
                            LspFactory.CreateTextEdit(context.SourceText.GetRange(new TextSpan(expression.Body.EndPosition, 0)), ")"),
                        ],
                    },
                },
            };

            var action = RazorCodeActionFactory.CreateUseExplicitExpression(workspaceEdit);
            return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([action]);
        }

        return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
    }
}
