// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionProvider)), Shared]
internal sealed class EscapeDocumentationCommentTerminatorCodeActionProvider : IRazorCodeActionProvider
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
                diagnosticId != RazorDiagnosticFactory.Parsing_DocumentationCommentTerminator.Id)
            {
                continue;
            }

            var span = context.SourceText.GetTextSpan(diagnostic.Range);
            if (!span.IntersectsWith(selection) ||
                syntaxTree.Root.FindToken(span.Start).Parent is not CSharpStatementLiteralSyntax content)
            {
                continue;
            }

            // End CDATA before the reference so XML decodes it, then resume the original section.
            var replacement = IsInCData(content, span.Start, cancellationToken)
                ? "*]]>&#47;<![CDATA["
                : "*&#47;";
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
                        Edits = [LspFactory.CreateTextEdit(diagnostic.Range, replacement)],
                    },
                },
            };

            var action = RazorCodeActionFactory.CreateEscapeDocumentationCommentTerminator(workspaceEdit);
            return Task.FromResult<ImmutableArray<RazorVSInternalCodeAction>>([action]);
        }

        return SpecializedTasks.EmptyImmutableArray<RazorVSInternalCodeAction>();
    }

    private static bool IsInCData(CSharpStatementLiteralSyntax content, int position, CancellationToken cancellationToken)
    {
        // Character references are literal text inside CDATA. Use Roslyn's XML-doc parser to find the
        // context, wrapping each line in "/// " so the offending "*/" cannot end the temporary comment.
        // The space keeps a body line starting with '/' from turning the prefix into an ordinary comment.
        const string commentPrefix = "/// ";
        var text = SourceText.From(content.GetContent());
        using var _ = StringBuilderPool.GetPooledObject(out var builder);
        foreach (var line in text.Lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            builder.Append(commentPrefix);
            builder.Append(text.ToString(line.SpanIncludingLineBreak));
        }

        var relativePosition = position - content.SpanStart;
        var projectedPosition = relativePosition + commentPrefix.Length * (text.Lines.GetLineFromPosition(relativePosition).LineNumber + 1);
        var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(builder.ToString()), cancellationToken: cancellationToken);
        var token = syntaxTree.GetRoot(cancellationToken).FindToken(projectedPosition, findInsideTrivia: true);
        return token.Parent?.FirstAncestorOrSelf<XmlCDataSectionSyntax>() is not null;
    }
}
