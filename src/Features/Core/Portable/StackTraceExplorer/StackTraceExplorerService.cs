// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

[ExportWorkspaceService(typeof(IStackTraceExplorerService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class StackTraceExplorerService() : IStackTraceExplorerService
{
    public (TextDocument? document, int line) GetDocumentAndLine(Solution solution, ParsedFrame frame)
    {
        if (frame is ParsedStackFrame parsedFrame)
        {
            var matches = GetFileMatches(solution, parsedFrame.Root, out var line);
            if (matches.IsEmpty)
            {
                return default;
            }

            return (matches[0], line);
        }

        return default;
    }

    public async Task<DefinitionItem?> TryFindDefinitionAsync(Solution solution, ParsedFrame frame, StackFrameSymbolPart symbolPart, CancellationToken cancellationToken)
    {
        if (frame is not ParsedStackFrame parsedFrame)
        {
            return null;
        }

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteStackTraceExplorerService, SerializableDefinitionItem?>(
                solution,
                (service, solutionInfo, cancellationToken) => service.TryFindDefinitionAsync(solutionInfo, parsedFrame.ToString(), symbolPart, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!result.HasValue)
            {
                return null;
            }

            var serializedDefinition = result.Value;
            if (!serializedDefinition.HasValue)
            {
                return null;
            }

            return await serializedDefinition.Value.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
        }

        return await StackTraceExplorerUtilities.GetDefinitionAsync(solution, parsedFrame.Root, symbolPart, cancellationToken).ConfigureAwait(false);
    }

    private static ImmutableArray<TextDocument> GetFileMatches(Solution solution, StackFrameCompilationUnit root, out int lineNumber)
    {
        lineNumber = 0;
        if (root.FileInformationExpression is null)
        {
            return [];
        }

        var fileName = root.FileInformationExpression.Path.ToString();
        var lineString = root.FileInformationExpression.Line.ToString();
        RoslynDebug.AssertNotNull(lineString);
        lineNumber = int.Parse(lineString);

        var documentId = solution.GetDocumentIdsWithFilePath(fileName).FirstOrDefault();

        if (documentId is not null)
        {
            var document = solution.GetRequiredTextDocument(documentId);
            return [document];
        }

        var documentName = Path.GetFileName(fileName);
        var potentialMatches = new HashSet<TextDocument>();

        foreach (var project in solution.Projects)
        {
            // As of writing there is no way to get all the documents for a specific project
            // so we need to check both the main and additional documents. If more document types
            // get added this likely will need to be updated.
            var allDocuments = project.Documents.Concat(project.AdditionalDocuments);

            foreach (var document in allDocuments)
            {
                var name = Path.GetFileName(document.Name);
                if (name.Equals(documentName, StringComparison.OrdinalIgnoreCase))
                {
                    potentialMatches.Add(document);
                }
            }
        }

        return [.. potentialMatches];
    }
}
