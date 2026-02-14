// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[ExportCSharpVisualBasicStatelessLspService(typeof(SimplifyMethodHandler)), Shared]
[Method(SimplifyMethodMethodName)]
internal class SimplifyMethodHandler : ILspServiceDocumentRequestHandler<SimplifyMethodParams, TextEdit[]?>
{
    public const string SimplifyMethodMethodName = "roslyn/simplifyMethod";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SimplifyMethodHandler()
    {
    }

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(SimplifyMethodParams request) => request.TextDocument;

    public async Task<TextEdit[]?> HandleRequestAsync(
        SimplifyMethodParams request,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        var originalDocument = context.Document;

        if (originalDocument is null)
            return null;

        var textEdit = request.TextEdit;

        return await GetSimplifiedEditsAsync(originalDocument, textEdit, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<TextEdit[]> GetSimplifiedEditsAsync(Document originalDocument, TextEdit textEdit, CancellationToken cancellationToken)
    {
        // Create a temporary syntax tree that includes the text edit.
        var originalSourceText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var pendingChange = ProtocolConversions.TextEditToTextChange(textEdit, originalSourceText);
        var newSourceText = originalSourceText.WithChanges(pendingChange);
        var originalTree = await originalDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var newTree = originalTree.WithChangedText(newSourceText);

        // Find the node that represents the text edit in the new syntax tree and annotate it for the simplifier.
        // Then create a document with a new syntax tree that has the annotated node.
        var node = newTree.FindNode(pendingChange.Span, findInTrivia: false, getInnermostNodeForTie: false, cancellationToken);
        var annotatedSyntaxRoot = newTree.GetRoot(cancellationToken).ReplaceNode(node, node.WithAdditionalAnnotations(Simplifier.Annotation));
        var annotatedDocument = originalDocument.WithSyntaxRoot(annotatedSyntaxRoot);

        // Call to the Simplifier and pass back the edits.
        var newDocument = await Simplifier.ReduceAsync(annotatedDocument, cancellationToken).ConfigureAwait(false);
        var changes = await newDocument.GetTextChangesAsync(originalDocument, cancellationToken).ConfigureAwait(false);
        return [.. changes.Select(change => ProtocolConversions.TextChangeToTextEdit(change, originalSourceText))];
    }
}
