// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal abstract class AbstractWarningStateMap<TWarningState>
        where TWarningState : struct
    {
        /// <summary>
        /// List of entries sorted in source order, each of which captures a
        /// position in the supplied syntax tree and the set of diagnostics (warnings)
        /// whose reporting should either be suppressed or enabled at this position.
        /// </summary>
        private readonly WarningStateMapEntry[] _warningStateMapEntries;

        protected AbstractWarningStateMap(SyntaxTree syntaxTree)
        {
            _warningStateMapEntries = CreateWarningStateMapEntries(syntaxTree);
        }

        /// <summary>
        /// Returns list of entries sorted in source order, each of which captures a
        /// position in the supplied syntax tree and the set of diagnostics (warnings)
        /// whose reporting should either be suppressed or enabled at this position.
        /// </summary>
        protected abstract WarningStateMapEntry[] CreateWarningStateMapEntries(SyntaxTree syntaxTree);

        /// <summary>
        /// Returns the reporting state for the supplied diagnostic id at the supplied position
        /// in the associated syntax tree.
        /// </summary>
        public TWarningState GetWarningState(string id, int position)
        {
            var entry = GetEntryAtOrBeforePosition(position);

            TWarningState state;
            if (entry.SpecificWarningOption.TryGetValue(id, out state))
            {
                return state;
            }

            return entry.GeneralWarningOption;
        }

        /// <summary>
        /// Gets the entry with the largest position less than or equal to supplied position.
        /// </summary>
        private WarningStateMapEntry GetEntryAtOrBeforePosition(int position)
        {
            Debug.Assert(_warningStateMapEntries != null && _warningStateMapEntries.Length > 0);
            int r = Array.BinarySearch(_warningStateMapEntries, new WarningStateMapEntry(position));
            return _warningStateMapEntries[r >= 0 ? r : ((~r) - 1)];
        }

        /// <summary>
        /// Struct that represents an entry in the warning state map. Sorts by position in the associated syntax tree.
        /// </summary>
        protected readonly struct WarningStateMapEntry : IComparable<WarningStateMapEntry>
        {
            // 0-based position in the associated syntax tree
            public readonly int Position;

            // the general option applicable to all warnings, accumulated of all #pragma up to the current Line.
            public readonly TWarningState GeneralWarningOption;

            // the mapping of the specific warning to the option, accumulated of all #pragma up to the current Line.
            public readonly ImmutableDictionary<string, TWarningState> SpecificWarningOption;

            public WarningStateMapEntry(int position)
            {
                this.Position = position;
                this.GeneralWarningOption = default;
                this.SpecificWarningOption = ImmutableDictionary.Create<string, TWarningState>();
            }

            public WarningStateMapEntry(int position, TWarningState general, ImmutableDictionary<string, TWarningState> specific)
            {
                this.Position = position;
                this.GeneralWarningOption = general;
                this.SpecificWarningOption = specific ?? ImmutableDictionary.Create<string, TWarningState>();
            }

            public int CompareTo(WarningStateMapEntry other)
            {
                return this.Position - other.Position;
            }
        }
    }
}
