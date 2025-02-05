// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

[ExportWorkspaceService(typeof(IDocumentTextDifferencingService), ServiceLayer.Default)]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultDocumentTextDifferencingService() : IDocumentTextDifferencingService
{
    public Task<ImmutableArray<TextChange>> GetTextChangesAsync(TextDocument oldDocument, TextDocument newDocument, CancellationToken cancellationToken)
        => GetTextChangesAsync(oldDocument, newDocument, TextDifferenceTypes.Word, cancellationToken);

    public Task<ImmutableArray<TextChange>> GetTextChangesAsync(TextDocument oldDocument, TextDocument newDocument, TextDifferenceTypes preferredDifferenceType, CancellationToken cancellationToken)
        => GetTextDocumentChangesAsync(oldDocument, newDocument, cancellationToken);

    /// <summary>
    /// Get the text changes between this document and a prior version of the same document.
    /// The changes, when applied to the text of the old document, will produce the text of the current document.
    /// </summary>
    private static async Task<ImmutableArray<TextChange>> GetTextDocumentChangesAsync(TextDocument oldTextDocument, TextDocument newTextDocument, CancellationToken cancellationToken)
    {
        if (oldTextDocument is Document oldDocument
            && newTextDocument is Document newDocument)
        {
            return [.. await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false)];
        }

        try
        {
            using (Logger.LogBlock(FunctionId.Workspace_Document_GetTextChanges, newTextDocument.Name, cancellationToken))
            {
                // no changes
                if (oldTextDocument == newTextDocument)
                    return [];

                if (newTextDocument.Id != oldTextDocument.Id)
                {
                    throw new ArgumentException(WorkspacesResources.The_specified_document_is_not_a_version_of_this_document);
                }

                var text = await newTextDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var oldText = await oldTextDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                return [.. text.GetTextChanges(oldText)];
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
