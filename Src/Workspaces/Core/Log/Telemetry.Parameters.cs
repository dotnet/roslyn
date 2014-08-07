// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log.Telemetry
{
    [Flags]
    internal enum UserActionOutcome
    {
        Committed = 0x1,
        Canceled = 0x2,
    }

    [Flags]
    internal enum RenameSessionExtendedFlags
    {
        None = 0x0,
        ConflictResolutionFinishedComputing = 0x1,
        PreviewChanges = 0x2,
    }

    internal struct RenameSessionInfo
    {
        public readonly OptionSet Options;
        public readonly UserActionOutcome Outcome;
        public readonly RenameSessionExtendedFlags ExtendedFlags;
        public readonly int RenamedIdentifiersWithoutConflicts;
        public readonly int ResolvableReferenceConflicts;
        public readonly int ResolvableNonReferenceConflicts;
        public readonly int UnresolvableConflicts;

        public RenameSessionInfo(
            OptionSet options,
            UserActionOutcome outcome,
            bool conflictResolutionFinishedComputing,
            bool previewChanges,
            int renamedIdentifiersWithoutConflicts,
            int resolvableReferenceConflicts,
            int resolvableNonReferenceConflicts,
            int unresolvableConflicts)
        {
            this.Options = options;
            this.Outcome = outcome;
            this.ExtendedFlags = RenameSessionExtendedFlags.None;
            this.ExtendedFlags |= conflictResolutionFinishedComputing ? RenameSessionExtendedFlags.ConflictResolutionFinishedComputing : RenameSessionExtendedFlags.None;
            this.ExtendedFlags |= previewChanges ? RenameSessionExtendedFlags.PreviewChanges : RenameSessionExtendedFlags.None;
            this.RenamedIdentifiersWithoutConflicts = renamedIdentifiersWithoutConflicts;
            this.ResolvableReferenceConflicts = resolvableReferenceConflicts;
            this.ResolvableNonReferenceConflicts = resolvableNonReferenceConflicts;
            this.UnresolvableConflicts = unresolvableConflicts;
        }
    }
}
