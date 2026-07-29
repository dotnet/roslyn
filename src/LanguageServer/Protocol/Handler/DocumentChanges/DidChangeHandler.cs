// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;

[ExportCSharpVisualBasicStatelessLspService(typeof(DidChangeHandler)), Shared]
[Method(Methods.TextDocumentDidChangeName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DidChangeHandler() : ILspServiceDocumentRequestHandler<DidChangeTextDocumentParams, object?>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => false;

    public TextDocumentIdentifier GetTextDocumentIdentifier(DidChangeTextDocumentParams request)
        => request.TextDocument;

    public async Task<object?> HandleRequestAsync(DidChangeTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var text = context.GetTrackedDocumentInfo(request.TextDocument.DocumentUri).SourceText;

        text = GetUpdatedSourceText(request.ContentChanges, text);

        context.UpdateTrackedDocument(request.TextDocument.DocumentUri, text, request.TextDocument.Version);

        return null;
    }

    internal static bool AreChangesInReverseOrder(SumType<TextDocumentContentChangePartial, TextDocumentContentChangeWholeDocument>[] contentChanges)
        => AreChangesInReverseOrder(contentChanges, startIndex: 0);

    private static bool AreChangesInReverseOrder(SumType<TextDocumentContentChangePartial, TextDocumentContentChangeWholeDocument>[] contentChanges, int startIndex)
    {
        for (var i = startIndex; i < contentChanges.Length; i++)
        {
            if (contentChanges[i].Value is not TextDocumentContentChangePartial)
            {
                return false;
            }
        }

        for (var i = startIndex + 1; i < contentChanges.Length; i++)
        {
            // The first loop verified that contentChanges[startIndex..] all contain TextDocumentContentChangePartial.
            var prevChange = (TextDocumentContentChangePartial)contentChanges[i - 1].Value!;
            var curChange = (TextDocumentContentChangePartial)contentChanges[i].Value!;

            if (prevChange.Range.Start.CompareTo(curChange.Range.End) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static SourceText GetUpdatedSourceText(SumType<TextDocumentContentChangePartial, TextDocumentContentChangeWholeDocument>[] contentChanges, SourceText text)
    {
        var firstRangedChangeIndex = 0;

        for (var i = contentChanges.Length - 1; i >= 0; i--)
        {
            if (contentChanges[i].Value is TextDocumentContentChangeWholeDocument wholeDocChange)
            {
                text = text.WithChanges(new TextChange(new TextSpan(0, text.Length), wholeDocChange.Text));
                firstRangedChangeIndex = i + 1;
                break;
            }
        }

        if (firstRangedChangeIndex == contentChanges.Length)
        {
            // No ranged edits follow the last full-document replacement (or there are no changes),
            // so return the already-updated text.
            return text;
        }

        // Per the LSP spec, each text change builds upon the previous, so we don't need to translate any text
        // positions between changes. See
        // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#didChangeTextDocumentParams
        // for more details.
        //
        // If the host sends us changes in a way such that no earlier change can affect the position of a later change,
        // then we can merge the changes into a single TextChange, allowing creation of only a single new
        // source text.
        if (AreChangesInReverseOrder(contentChanges, firstRangedChangeIndex))
        {
            // The changes were in reverse document order, so we can merge them into a single operation on the source text.
            // Note that the WithChanges implementation works more efficiently with it's input in forward document order.
            var newChanges = new TextChange[contentChanges.Length - firstRangedChangeIndex];
            for (var i = contentChanges.Length - 1; i >= firstRangedChangeIndex; i--)
            {
                newChanges[contentChanges.Length - 1 - i] = ProtocolConversions.ContentChangeEventToTextChange((TextDocumentContentChangePartial)contentChanges[i].Value!, text);
            }

            text = text.WithChanges(newChanges);
        }
        else
        {
            // The host didn't send us the items ordered, so we'll apply each one independently.
            for (var i = firstRangedChangeIndex; i < contentChanges.Length; i++)
            {
                var change = (TextDocumentContentChangePartial)contentChanges[i].Value!;
                text = text.WithChanges(ProtocolConversions.ContentChangeEventToTextChange(change, text));
            }
        }

        return text;
    }
}
