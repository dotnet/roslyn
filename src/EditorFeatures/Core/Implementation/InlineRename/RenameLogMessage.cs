// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private const string Committed = nameof(Committed);
        private const string Canceled = nameof(Canceled);

        private const string ConflictResolutionFinishedComputing = nameof(ConflictResolutionFinishedComputing);
        private const string PreviewChanges = nameof(PreviewChanges);

        private const string RenamedIdentifiersWithoutConflicts = nameof(RenamedIdentifiersWithoutConflicts);
        private const string ResolvableReferenceConflicts = nameof(ResolvableReferenceConflicts);
        private const string ResolvableNonReferenceConflicts = nameof(ResolvableNonReferenceConflicts);
        private const string UnresolvableConflicts = nameof(UnresolvableConflicts);

        public static KeyValueLogMessage Create(
            OptionSet optionSet, UserActionOutcome outcome,
            bool conflictResolutionFinishedComputing, bool previewChanges,
            IList<InlineRenameReplacementKind> replacementKinds)
        {
            return KeyValueLogMessage.Create(m =>
            {
                m[RenameInComments] = optionSet.GetOption(RenameOptions.RenameInComments);
                m[RenameInStrings] = optionSet.GetOption(RenameOptions.RenameInStrings);
                m[RenameOverloads] = optionSet.GetOption(RenameOptions.RenameOverloads);

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
