// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
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

    public Task<object?> HandleRequestAsync(DidChangeTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var text = context.GetTrackedDocumentSourceText(request.TextDocument.Uri);

        text = GetUpdatedSourceText(request.ContentChanges, text);

        context.UpdateTrackedDocument(request.TextDocument.Uri, text);

        return SpecializedTasks.Default<object>();
    }

    internal static bool AreChangesInReverseOrder(TextDocumentContentChangeEvent[] contentChanges)
    {
        for (var i = 1; i < contentChanges.Length; i++)
        {
            var prevChange = contentChanges[i - 1];
            var curChange = contentChanges[i];

            if (prevChange.Range.Start.CompareTo(curChange.Range.End) < 0)
            {
                return false;
            }
        }

        return true;
    }

    private static SourceText GetUpdatedSourceText(TextDocumentContentChangeEvent[] contentChanges, SourceText text)
    {
        // Per the LSP spec, each text change builds upon the previous, so we don't need to translate any text
        // positions between changes. See
        // https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#didChangeTextDocumentParams
        // for more details.
        //
        // If the host sends us changes in a way such that no earlier change can affect the position of a later change,
        // then we can merge the changes into a single TextChange, allowing creation of only a single new
        // source text.
        if (AreChangesInReverseOrder(contentChanges))
        {
            // The changes were in reverse document order, so we can merge them into a single operation on the source text.
            // Note that the WithChanges implementation works more efficiently with it's input in forward document order.
            var newChanges = contentChanges.Reverse().SelectAsArray(change => ProtocolConversions.ContentChangeEventToTextChange(change, text));
            text = text.WithChanges(newChanges);
        }
        else
        {
            // The host didn't send us the items ordered, so we'll apply each one independently.
            foreach (var change in contentChanges)
                text = text.WithChanges(ProtocolConversions.ContentChangeEventToTextChange(change, text));
        }

        return text;
    }
}
