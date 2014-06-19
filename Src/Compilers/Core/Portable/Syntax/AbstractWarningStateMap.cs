// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal abstract class AbstractWarningStateMap
    {
        /// <summary>
        /// Returns the reporting level of the given diagnostic id at the specified position
        /// in the associated syntax tree.
        /// </summary>
        public abstract ReportDiagnostic GetWarningState(string id, int position);

        /// <summary>
        /// Gets the position mapped entry in the provided sorted array with the largest position less than or equal to position
        /// </summary>
        protected static WarningStateMapEntry GetEntryAtOrBeforePosition(WarningStateMapEntry[] sortedEntries, int position)
        {
            Debug.Assert(sortedEntries != null && sortedEntries.Length > 0);
            int r = Array.BinarySearch(sortedEntries, new WarningStateMapEntry(position));
            return sortedEntries[r >= 0 ? r : ((~r) - 1)];
        }

        /// <summary>
        /// Struct that represents an entry in the warning state map. Sorts by position in the associated syntax tree.
        /// </summary>
        protected struct WarningStateMapEntry : IComparable<WarningStateMapEntry>
        {
            // 0-based position in the associated syntax tree
            public readonly int Position;

            // the general option applicable to all warnings, accumulated of all #pragma up to the current Line.
            public readonly ReportDiagnostic GeneralWarningOption;

            // the mapping of the specific warning to the option, accumulated of all #pragma up to the current Line.
            public readonly ImmutableDictionary<string, ReportDiagnostic> SpecificWarningOption;

            public WarningStateMapEntry(int position)
            {
                this.Position = position;
                this.GeneralWarningOption = ReportDiagnostic.Default;
                this.SpecificWarningOption = ImmutableDictionary.Create<string, ReportDiagnostic>();
            }

            public WarningStateMapEntry(int position, ReportDiagnostic general, ImmutableDictionary<string, ReportDiagnostic> specific)
            {
                this.Position = position;
                this.GeneralWarningOption = general;
                this.SpecificWarningOption = specific ?? ImmutableDictionary.Create<string, ReportDiagnostic>();
            }

            public int CompareTo(WarningStateMapEntry other)
            {
                return this.Position - other.Position;
            }
        }
    }
}
