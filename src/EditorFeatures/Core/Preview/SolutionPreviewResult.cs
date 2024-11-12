// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor;

internal class SolutionPreviewResult(
    IThreadingContext threadingContext,
    IList<SolutionPreviewItem>? previews,
    SolutionChangeSummary? changeSummary = null)
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IList<SolutionPreviewItem> _previews = previews ?? SpecializedCollections.EmptyList<SolutionPreviewItem>();
    public readonly SolutionChangeSummary? ChangeSummary = changeSummary;

    public SolutionPreviewResult(IThreadingContext threadingContext, SolutionPreviewItem preview, SolutionChangeSummary? changeSummary = null)
        : this(threadingContext, [preview], changeSummary)
    {
    }

    public bool IsEmpty => _previews.Count == 0;

    public async Task<IReadOnlyList<object>?> GetPreviewsAsync(DocumentId? preferredDocumentId = null, ProjectId? preferredProjectId = null, CancellationToken cancellationToken = default)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        cancellationToken.ThrowIfCancellationRequested();

        var orderedPreviews = _previews.OrderBy((i1, i2) =>
        {
            return i1.DocumentId == preferredDocumentId && i2.DocumentId != preferredDocumentId ? -1 :
                   i1.DocumentId != preferredDocumentId && i2.DocumentId == preferredDocumentId ? 1 :
                   _previews.IndexOf(i1) - _previews.IndexOf(i2);
        }).ThenBy((i1, i2) =>
        {
            return i1.ProjectId == preferredProjectId && i2.ProjectId != preferredProjectId ? -1 :
                   i1.ProjectId != preferredProjectId && i2.ProjectId == preferredProjectId ? 1 :
                   _previews.IndexOf(i1) - _previews.IndexOf(i2);
        }).ThenBy((i1, i2) =>
        {
            return i1.Text == null && i2.Text != null ? -1 :
                   i1.Text != null && i2.Text == null ? 1 :
                   _previews.IndexOf(i1) - _previews.IndexOf(i2);
        });

        var result = new List<object>();
        var gotRichPreview = false;

        try
        {
            foreach (var previewItem in _previews)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (previewItem.Text != null)
                {
                    result.Add(previewItem.Text);
                }
                else if (!gotRichPreview)
                {
                    var preview = await previewItem.LazyPreview(cancellationToken).ConfigureAwait(true);
                    if (preview != null)
                    {
                        result.Add(preview);
                        gotRichPreview = true;
                    }
                }
            }

            return result.Count == 0 ? null : result;
        }
        catch (OperationCanceledException)
        {
            // make sure we dispose all disposable preview objects before
            // we let control to exit this method
            result.OfType<IDisposable>().Do(d => d.Dispose());
            throw;
        }
    }

    /// <summary>Merge two different previews into one final preview result.  The final preview will
    /// have a concatenation of all the inidivual previews contained within each result.</summary>
    internal static SolutionPreviewResult? Merge(SolutionPreviewResult? result1, SolutionPreviewResult? result2)
    {
        if (result1 == null)
        {
            return result2;
        }

        if (result2 == null)
        {
            return result1;
        }

        return new SolutionPreviewResult(
            result1._threadingContext,
            result1._previews.Concat(result2._previews).ToList(),
            result1.ChangeSummary ?? result2.ChangeSummary);
    }
}
