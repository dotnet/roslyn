// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
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

    internal enum LightBulbInvocationType
    {
        Keyboard,
        Mouse
    }

    internal struct LightBulbSessionInfo
    {
        public readonly LightBulbInvocationType LightBulbInvocationType;
        public readonly UserActionOutcome Outcome;
        public readonly int NumberOfItemsShown;
        public readonly int IndexOfSelectedItem;
        public readonly int UniqueIdOfSelectedItem;
        public readonly int ExtendedFlagsOfSelectedItem;

        public LightBulbSessionInfo(
            LightBulbInvocationType lightBulbInvocationType,
            UserActionOutcome outcome,
            int numberOfItemsShown,
            int indexOfSelectedItem,
            int uniqueIdOfSelectedItem,
            int extendedFlagsOfSelectedItem)
        {
            this.LightBulbInvocationType = lightBulbInvocationType;
            this.Outcome = outcome;
            this.NumberOfItemsShown = numberOfItemsShown;
            this.IndexOfSelectedItem = indexOfSelectedItem;
            this.UniqueIdOfSelectedItem = uniqueIdOfSelectedItem;
            this.ExtendedFlagsOfSelectedItem = extendedFlagsOfSelectedItem;
        }
    }
}
