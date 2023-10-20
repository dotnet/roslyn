// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MapCode;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(MapCodeHandler)), Shared]
[Method(WorkspaceMapCodeName)]
internal sealed partial class MapCodeHandler : ILspServiceRequestHandler<MapCodeParams, LSP.WorkspaceEdit?>
{
    public const string WorkspaceMapCodeName = "workspace/mapCode";

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public MapCodeHandler()
    {
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public async Task<WorkspaceEdit?> HandleRequestAsync(MapCodeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        //TODO: handle request.Updates if not empty
        Contract.ThrowIfNull(context.Solution);

        using var _ = PooledDictionary<Uri, TextEdit[]>.GetInstance(out var uriToEditsMap);
        foreach (var codeMapping in request.Mappings)
        {
            var mappingResult = await MapCodeAsync(codeMapping).ConfigureAwait(false);

            // Assume no two codeMappings target a common document.
            if (mappingResult is (Uri uri, TextEdit[] textEdits))
                uriToEditsMap[uri] = textEdits;
        }

        // return a combined WorkspaceEdit
        if (context.GetRequiredClientCapabilities().Workspace?.WorkspaceEdit?.DocumentChanges is true)
        {
            return new WorkspaceEdit
            {
                DocumentChanges = uriToEditsMap.Select(kvp => new TextDocumentEdit
                {
                    TextDocument = new OptionalVersionedTextDocumentIdentifier { Uri = kvp.Key },
                    Edits = kvp.Value,
                }).ToArray()
            };
        }
        else
        {
            return new WorkspaceEdit
            {
                Changes = uriToEditsMap.ToDictionary(kvp => ProtocolConversions.GetDocumentFilePathFromUri(kvp.Key), kvp => kvp.Value)
            };
        }

        async Task<(Uri, TextEdit[])?> MapCodeAsync(LSP.MapCodeMapping codeMapping)
        {
            var textDocument = codeMapping.TextDocument;
            if (textDocument == null)
                return null;

            if (context.Solution.GetDocument(textDocument) is not Document document)
                return null;

            var codeMapper = document.GetLanguageService<IMapCodeService>();
            if (codeMapper == null)
                return null;

            var focusLocations = await ConvertFocusLocationsToDocumentAndSpansAsync(
                document,
                textDocument,
                codeMapping.FocusLocations,
                cancellationToken).ConfigureAwait(false);

            var newDocument = await codeMapper.MapCodeAsync(
                document,
                codeMapping.Contents.ToImmutableArrayOrEmpty(),
                focusLocations,
                formatMappedCode: true,
                cancellationToken).ConfigureAwait(false);

            if (newDocument is null)
                return null;

            var textChanges = (await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false)).ToImmutableArray();
            if (textChanges.IsEmpty)
                return null;

            var oldText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textEdits = textChanges.Select(change => ProtocolConversions.TextChangeToTextEdit(change, oldText)).ToArray();

            return (newDocument.GetURI(), textEdits);
        }

        static async Task<ImmutableArray<(Document, TextSpan)>> ConvertFocusLocationsToDocumentAndSpansAsync(
            Document document, TextDocumentIdentifier textDocumentIdentifier, LSP.Location[][]? focusLocations, CancellationToken cancellationToken)
        {
            if (focusLocations is null)
                return ImmutableArray<(Document, TextSpan)>.Empty;

            using var _ = ArrayBuilder<(Document, TextSpan)>.GetInstance(out var builder);
            foreach (var locationsOfSamePriority in focusLocations)
            {
                foreach (var location in locationsOfSamePriority)
                {
                    // Ignore anything not in target document, which current code mapper doesn't handle anyway
                    if (!location.Uri.Equals(textDocumentIdentifier.Uri))
                        continue;

                    if (document.Project.Solution.GetDocument(textDocumentIdentifier) is Document focusDocument)
                    {
                        var focusText = await focusDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        builder.Add((focusDocument, ProtocolConversions.RangeToTextSpan(location.Range, focusText)));
                    }
                }
            }

            return builder.ToImmutable();
        }
    }
}
