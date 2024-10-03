// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(SourceGeneratedDocumentGetTextHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SourceGeneratedDocumentGetTextHandler() : ILspServiceDocumentRequestHandler<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>
{
    public const string MethodName = "sourceGeneratedDocument/_roslyn_getText";

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(SourceGeneratorGetTextParams request) => request.TextDocument;

    public async Task<SourceGeneratedDocumentText> HandleRequestAsync(SourceGeneratorGetTextParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;

        // Nothing here strictly prevents this from working on any other document, but we'll assert we got a source-generated file, since
        // it wouldn't really make sense for the server to be asked for the contents of a regular file. Since this endpoint is intended for
        // source-generated files only, this would indicate that something else has gone wrong.
        Contract.ThrowIfFalse(document is SourceGeneratedDocument);

        // When a user has a open source-generated file, we ensure that the contents in the LSP snapshot match the contents that we
        // get through didOpen/didChanges, like any other file. That way operations in LSP file are in sync with the
        // contents the user has. However in this case, we don't want to look at that frozen text, but look at what the
        // generator would generate if we ran it again. Otherwise, we'll get "stuck" and never update the file with something new.
        document = await document.Project.Solution.WithoutFrozenSourceGeneratedDocuments().GetDocumentAsync(document.Id, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

        var text = document != null ? await document.GetTextAsync(cancellationToken).ConfigureAwait(false) : null;
        return new SourceGeneratedDocumentText(text?.ToString());
    }
}
