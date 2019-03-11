// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Syntax
{
    internal abstract class AbstractWarningStateMap<WarningState>
    {
        /// <summary>
        /// List of entries sorted in source order, each of which captures a
        /// position in the supplied syntax tree and the set of diagnostics (warnings)
        /// whose reporting should either be suppressed or enabled at this position.
        /// </summary>
        private readonly WarningStateMapEntry[] _warningStateMapEntries;

        /// <summary>
        /// Records if this state map is for generated code, which can have differing semantics in some cases
        /// </summary>
        protected readonly bool _isGeneratedCode;

        protected AbstractWarningStateMap(SyntaxTree syntaxTree, bool isGeneratedCode)
        {
            _isGeneratedCode = isGeneratedCode;
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
        public WarningState GetWarningState(string id, int position)
        {
            var entry = GetEntryAtOrBeforePosition(position);

            WarningState state;
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
        protected struct WarningStateMapEntry : IComparable<WarningStateMapEntry>
        {
            // 0-based position in the associated syntax tree
            public readonly int Position;

            // the general option applicable to all warnings, accumulated of all #pragma up to the current Line.
            public readonly WarningState GeneralWarningOption;

            // the mapping of the specific warning to the option, accumulated of all #pragma up to the current Line.
            public readonly ImmutableDictionary<string, WarningState> SpecificWarningOption;

            public WarningStateMapEntry(int position)
            {
                this.Position = position;
                this.GeneralWarningOption = default;
                this.SpecificWarningOption = ImmutableDictionary.Create<string, WarningState>();
            }

            public WarningStateMapEntry(int position, WarningState general, ImmutableDictionary<string, WarningState> specific)
            {
                this.Position = position;
                this.GeneralWarningOption = general;
                this.SpecificWarningOption = specific ?? ImmutableDictionary.Create<string, WarningState>();
            }

            public int CompareTo(WarningStateMapEntry other)
            {
                return this.Position - other.Position;
            }
        }
    }
}
