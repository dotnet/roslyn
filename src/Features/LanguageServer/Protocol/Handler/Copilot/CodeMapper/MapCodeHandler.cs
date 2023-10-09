// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeMapping;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(MapCodeHandler)), Shared]
[Method(LSP.MapperMethods.TextDocumentMapCodeName)]
internal sealed partial class MapCodeHandler : ILspServiceDocumentRequestHandler<MapCodeParams, LSP.WorkspaceEdit?>
{
    
    public const string TextDocumentMapCodeName = "textDocument/mapCode";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MapCodeHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;


    public TextDocumentIdentifier GetTextDocumentIdentifier(MapCodeParams request)
        => request.TextDocument;

    public async Task<WorkspaceEdit?> HandleRequestAsync(MapCodeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        Contract.ThrowIfNull(document);

        var codeMapper = document.GetLanguageService<IMapCodeService>();
        if (codeMapper == null)
            return null;

        //TODO: handle request.Updates if not empty

        using var _ = ArrayBuilder<DocumentSpan>.GetInstance(out var focusLocations);
        foreach (var location in request.FocusLocations.ToImmutableArrayOrEmpty())
        {
            if (document.Project.Solution.GetDocument(request.TextDocument) is Document focusDocument)
            {
                var focusText = await focusDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                focusLocations.Add(new(document: focusDocument, sourceSpan: ProtocolConversions.RangeToTextSpan(location.Range, focusText)));
            }
        }

        var newDocument = await codeMapper.MapCodeAsync(document, request.Contents.ToImmutableArrayOrEmpty(), focusLocations.ToImmutable(), true, cancellationToken).ConfigureAwait(false);
        var textChanges = (await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false)).ToImmutableArray();
        if (textChanges.IsEmpty)
            return null;

        var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var textEdits = textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, oldText)).ToArray();

        if (context.GetRequiredClientCapabilities().Workspace?.WorkspaceEdit?.DocumentChanges is true)
        {
            return new WorkspaceEdit
            {
                DocumentChanges = new[]
                {
                    new TextDocumentEdit
                    {
                        TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = document.GetURI() },
                        Edits = textEdits,
                    }
                }
            };
        }
        else
        {
            return new WorkspaceEdit
            {
                Changes = new Dictionary<string, TextEdit[]>
                {
                    { document.GetURI().AbsolutePath, textEdits }
                }
            };
        }
    }
}
