// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class SolutionPreviewResult
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

            var preview = await previewItem.LazyPreview(cancellationToken).ConfigureAwait(true);
            if (preview == null)
            {
                return await TakeNextPreviewAsync(cancellationToken: cancellationToken).ConfigureAwait(true);
            }

            return preview;
        }
    }
}
