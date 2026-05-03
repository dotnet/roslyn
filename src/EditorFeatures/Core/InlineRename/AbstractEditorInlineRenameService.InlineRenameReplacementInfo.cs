// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Rename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractEditorInlineRenameService : IEditorInlineRenameService
{
    private sealed class InlineRenameReplacementInfo : IInlineRenameReplacementInfo
    {
        private readonly ConflictResolution _conflicts;

        public InlineRenameReplacementInfo(ConflictResolution conflicts)
        {
            Contract.ThrowIfFalse(conflicts.IsSuccessful);
            _conflicts = conflicts;
        }

        public IEnumerable<DocumentId> DocumentIds => _conflicts.DocumentIds;

        public Solution NewSolution => _conflicts.NewSolution!;

        public bool ReplacementTextValid => _conflicts.ReplacementTextValid;

        public IEnumerable<InlineRenameReplacement> GetReplacements(DocumentId documentId)
        {
            var nonComplexifiedSpans = GetNonComplexifiedReplacements(documentId);
            var complexifiedSpans = GetComplexifiedReplacements(documentId);

            return nonComplexifiedSpans.Concat(complexifiedSpans);
        }

        private IEnumerable<InlineRenameReplacement> GetNonComplexifiedReplacements(DocumentId documentId)
        {
            var modifiedSpans = _conflicts.GetModifiedSpanMap(documentId);
            var locationsForDocument = _conflicts.GetRelatedLocationsForDocument(documentId);

            // The RenamedSpansTracker doesn't currently track unresolved conflicts for
            // unmodified locations.  If the document wasn't modified, we can just use the 
            // original span as the new span, but otherwise we need to filter out 
            // locations that aren't in modifiedSpans. 
            if (modifiedSpans.Any())
            {
                return locationsForDocument.Where(loc => modifiedSpans.ContainsKey(loc.ConflictCheckSpan))
                                           .Select(loc => new InlineRenameReplacement(loc, modifiedSpans[loc.ConflictCheckSpan]));
            }
            else
            {
                return locationsForDocument.Select(loc => new InlineRenameReplacement(loc, loc.ConflictCheckSpan));
            }
        }

        private IEnumerable<InlineRenameReplacement> GetComplexifiedReplacements(DocumentId documentId)
        {
            return _conflicts.GetComplexifiedSpans(documentId)
                .Select(s => new InlineRenameReplacement(InlineRenameReplacementKind.Complexified, s.oldSpan, s.newSpan));
        }
    }
}
