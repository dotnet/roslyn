// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal static class RenameLogMessage
    {
        private const string RenameInComments = nameof(RenameInComments);
        private const string RenameInStrings = nameof(RenameInStrings);
        private const string RenameOverloads = nameof(RenameOverloads);
        private const string RenameFile = nameof(RenameFile);

        private const string Committed = nameof(Committed);
        private const string Canceled = nameof(Canceled);

        private const string ConflictResolutionFinishedComputing = nameof(ConflictResolutionFinishedComputing);
        private const string PreviewChanges = nameof(PreviewChanges);

        private const string RenamedIdentifiersWithoutConflicts = nameof(RenamedIdentifiersWithoutConflicts);
        private const string ResolvableReferenceConflicts = nameof(ResolvableReferenceConflicts);
        private const string ResolvableNonReferenceConflicts = nameof(ResolvableNonReferenceConflicts);
        private const string UnresolvableConflicts = nameof(UnresolvableConflicts);

        public static KeyValueLogMessage Create(
            SymbolRenameOptions options, UserActionOutcome outcome,
            bool conflictResolutionFinishedComputing, bool previewChanges,
            IList<InlineRenameReplacementKind> replacementKinds)
        {
            return KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m[RenameInComments] = options.RenameInComments;
                m[RenameInStrings] = options.RenameInStrings;
                m[RenameOverloads] = options.RenameOverloads;
                m[RenameFile] = options.RenameFile;

                m[Committed] = (outcome & UserActionOutcome.Committed) == UserActionOutcome.Committed;
                m[Canceled] = (outcome & UserActionOutcome.Canceled) == UserActionOutcome.Canceled;

                m[ConflictResolutionFinishedComputing] = conflictResolutionFinishedComputing;
                m[PreviewChanges] = previewChanges;

                m[RenamedIdentifiersWithoutConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.NoConflict);
                m[ResolvableReferenceConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.ResolvedReferenceConflict);
                m[ResolvableNonReferenceConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.ResolvedNonReferenceConflict);
                m[UnresolvableConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.UnresolvedConflict);
            });
        }

        [Flags]
        public enum UserActionOutcome
        {
            Committed = 0x1,
            Canceled = 0x2,
        }
    }
}
