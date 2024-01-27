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

[ExportCSharpVisualBasicStatelessLspService(typeof(SourceGeneratedFileGetTextHandler)), Shared]
[Method("sourceGeneratedFile/_roslyn_getText")]
internal sealed class SourceGeneratedFileGetTextHandler : ILspServiceDocumentRequestHandler<SourceGeneratorGetTextParams, SourceGeneratedDocumentText>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SourceGeneratedFileGetTextHandler()
    {
    }

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
        return new SourceGeneratedDocumentText { Text = (await document.GetTextAsync(cancellationToken).ConfigureAwait(false)).ToString() };
    }
}
