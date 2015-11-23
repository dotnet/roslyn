// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractEditorInlineRenameService : IEditorInlineRenameService
    {
        private class InlineRenameReplacementInfo : IInlineRenameReplacementInfo
        {
            private readonly ConflictResolution _conflicts;

            public InlineRenameReplacementInfo(ConflictResolution conflicts)
            {
                _conflicts = conflicts;
            }

            public IEnumerable<DocumentId> DocumentIds
            {
                get
                {
                    return _conflicts.DocumentIds.Concat(_conflicts.RelatedLocations.Select(l => l.DocumentId)).Distinct();
                }
            }

            public Solution NewSolution
            {
                get
                {
                    return _conflicts.NewSolution;
                }
            }

            public IEnumerable<RelatedLocationType> Resolutions
            {
                get
                {
                    return _conflicts.RelatedLocations.Select(loc => loc.Type);
                }
            }

            public bool ReplacementTextValid
            {
                get
                {
                    return _conflicts.ReplacementTextValid;
                }
            }

            public IEnumerable<InlineRenameReplacement> GetReplacements(DocumentId documentId)
            {
                var nonComplexifiedSpans = GetNonComplexifiedReplacements(documentId);
                var complexifiedSpans = GetComplexifiedReplacements(documentId);

                return nonComplexifiedSpans.Concat(complexifiedSpans);
            }

            private IEnumerable<InlineRenameReplacement> GetNonComplexifiedReplacements(DocumentId documentId)
            {
                var modifiedSpans = _conflicts.RenamedSpansTracker.GetModifiedSpanMap(documentId);
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
                return _conflicts.RenamedSpansTracker.GetComplexifiedSpans(documentId)
                    .Select(s => new InlineRenameReplacement(InlineRenameReplacementKind.Complexified, s.Item1, s.Item2));
            }
        }
    }
}
