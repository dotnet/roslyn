// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class SolutionPreviewResult : ForegroundThreadAffinitizedObject
    {
        private IList<SolutionPreviewItem> _previews = null;
        public readonly SolutionChangeSummary ChangeSummary;

        public SolutionPreviewResult(IList<SolutionPreviewItem> previews, SolutionChangeSummary changeSummary = null)
        {
            _previews = previews;
            this.ChangeSummary = changeSummary;
        }

        public bool IsEmpty
        {
            get { return (_previews == null) || (_previews.Count == 0); }
        }

        /// <remarks>
        /// Once a preview object is returned from this function, the ownership of this preview object is
        /// transferred to the caller. It is the caller's responsibility to ensure that the preview object
        /// will be properly disposed (i.e. that any contained IWpfTextViews will be properly closed).
        ///
        /// This function guarantees that it will not return the same preview object twice if called twice
        /// (thereby reducing the possibility that a given preview object can end up with more than one owner).
        /// </remarks>
        public async Task<object> TakeNextPreviewAsync(DocumentId preferredDocumentId = null, ProjectId preferredProjectId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            AssertIsForeground();

            cancellationToken.ThrowIfCancellationRequested();

            if (IsEmpty)
            {
                return null;
            }

            SolutionPreviewItem previewItem = null;

            // Check if we have a preview for some change within the supplied preferred document.
            if (preferredDocumentId != null)
            {
                previewItem = _previews.Where(p => p.DocumentId == preferredDocumentId).FirstOrDefault();
            }

            // Check if we have a preview for some change within the supplied preferred project.
            if ((previewItem == null) && (preferredProjectId != null))
            {
                previewItem = _previews.Where(p => p.ProjectId == preferredProjectId).FirstOrDefault();
            }

            // We don't have a preview matching the preferred document or project. Return the first preview.
            if (previewItem == null)
            {
                previewItem = _previews.FirstOrDefault();
            }

            cancellationToken.ThrowIfCancellationRequested();

            // We should now remove this preview object from the list so that it can not be returned again
            // if someone calls this function again (thereby reducing the possibility that a given preview
            // object can end up with more than one owner - see <remarks> above).
            _previews.Remove(previewItem);

            // We use ConfigureAwait(true) to stay on the UI thread.
            var preview = await previewItem.LazyPreview(cancellationToken).ConfigureAwait(true);
            if (preview == null)
            {
                // Keep going if preview is null. Null preview indicates that although the preferred document was marked as changed, 
                // there are no textual changes in the preferred document and so we can't create a diff preview for this document.
                // This can happen in the case of the 'rename tracking' code fix - the document where the fix was triggered from (i.e.
                // the preferred document) is always reported as changed (on account of difference in document version). However, if
                // the renamed identifier is not referenced from any other location in this document, then there will be no text changes
                // between the two versions. In such cases, We should keep going until we find a document with text changes that can be
                // diffed and previewed.

                // There is no danger of infinite recursion here since we remove null previews from the list each time.
                preview = await TakeNextPreviewAsync(preferredDocumentId, preferredProjectId, cancellationToken).ConfigureAwait(true);
            }

            return preview;
        }
    }
}
