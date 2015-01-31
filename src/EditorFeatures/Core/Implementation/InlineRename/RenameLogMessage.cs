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
        private const string RenameInComments = "RenameInComments";
        private const string RenameInStrings = "RenameInStrings";
        private const string RenameOverloads = "RenameOverloads";

        private const string Committed = "Committed";
        private const string Canceled = "Canceled";

        private const string ConflictResolutionFinishedComputing = "ConflictResolutionFinishedComputing";
        private const string PreviewChanges = "PreviewChanges";

        private const string RenamedIdentifiersWithoutConflicts = "RenamedIdentifiersWithoutConflicts";
        private const string ResolvableReferenceConflicts = "ResolvableReferenceConflicts";
        private const string ResolvableNonReferenceConflicts = "ResolvableNonReferenceConflicts";
        private const string UnresolvableConflicts = "UnresolvableConflicts";

        public static KeyValueLogMessage Create(
            OptionSet optionSet, UserActionOutcome outcome,
            bool conflictResolutionFinishedComputing, bool previewChanges,
            IList<InlineRenameReplacementKind> replacementKinds)
        {
            return KeyValueLogMessage.Create(m =>
            {
                m[RenameInComments] = optionSet.GetOption(RenameOptions.RenameInComments).ToString();
                m[RenameInStrings] = optionSet.GetOption(RenameOptions.RenameInStrings).ToString();
                m[RenameOverloads] = optionSet.GetOption(RenameOptions.RenameOverloads).ToString();

                m[Committed] = ((outcome & UserActionOutcome.Committed) == UserActionOutcome.Committed).ToString();
                m[Canceled] = ((outcome & UserActionOutcome.Canceled) == UserActionOutcome.Canceled).ToString();

                m[ConflictResolutionFinishedComputing] = conflictResolutionFinishedComputing.ToString();
                m[PreviewChanges] = previewChanges.ToString();

                m[RenamedIdentifiersWithoutConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.NoConflict).ToString();
                m[ResolvableReferenceConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.ResolvedReferenceConflict).ToString();
                m[ResolvableNonReferenceConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.ResolvedNonReferenceConflict).ToString();
                m[UnresolvableConflicts] = replacementKinds.Count(r => r == InlineRenameReplacementKind.UnresolvedConflict).ToString();
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
