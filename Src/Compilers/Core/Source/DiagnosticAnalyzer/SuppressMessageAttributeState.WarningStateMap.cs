// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class SuppressMessageAttributeState
    {
        private class WarningStateMap : AbstractWarningStateMap
        {
            private WarningStateMapEntry[] warningStateMapEntries;
            private ConcurrentSet<WarningStateMapBuilderEntry> warningStateMapBuilderEntries;

            public WarningStateMap()
            {
                this.warningStateMapBuilderEntries = new ConcurrentSet<WarningStateMapBuilderEntry>();
            }

            public override ReportDiagnostic GetWarningState(string id, int position)
            {
                if (this.warningStateMapEntries == null)
                {
                    Interlocked.CompareExchange(ref this.warningStateMapEntries, CoalesceBuilderEntries(this.warningStateMapBuilderEntries.ToImmutableArray()), null);
                }

                var entry = GetEntryAtOrBeforePosition(this.warningStateMapEntries, position);

                ReportDiagnostic report;
                if (entry.SpecificWarningOption.TryGetValue(id, out report))
                {
                    return report;
                }

                return entry.GeneralWarningOption;
            }

            public void AddSuppression(string id, TextSpan span)
            {
                this.warningStateMapBuilderEntries.Add(new WarningStateMapBuilderEntry(span.Start, id, ReportDiagnostic.Suppress));
                this.warningStateMapBuilderEntries.Add(new WarningStateMapBuilderEntry(span.End, id, ReportDiagnostic.Default));
            }

            private WarningStateMapEntry[] CoalesceBuilderEntries(ImmutableArray<WarningStateMapBuilderEntry> builderEntries)
            {
                var builder = new ArrayBuilder<WarningStateMapEntry>();

                int currentPosition = 0;
                var accumulatedSpecificWarningState = ImmutableDictionary<string, ReportDiagnostic>.Empty;

                foreach (var entry in builderEntries.Sort())
                {
                    if (entry.Position != currentPosition)
                    {
                        // Commit the previous map entry
                        builder.Add(new WarningStateMapEntry(currentPosition, ReportDiagnostic.Default, accumulatedSpecificWarningState));

                        // Start building up a new map entry
                        currentPosition = entry.Position;
                    }

                    accumulatedSpecificWarningState = accumulatedSpecificWarningState.SetItem(entry.Id, entry.SpecificWarningOption);
                }

                // Commit the final map entry
                builder.Add(new WarningStateMapEntry(currentPosition, ReportDiagnostic.Default, accumulatedSpecificWarningState));

                var entries = builder.ToArrayAndFree();
#if DEBUG
                // Make sure the entries array is correctly sorted. 
                for (int i = 1; i < entries.Length - 1; ++i)
                {
                    Debug.Assert(entries[i].CompareTo(entries[i + 1]) < 0);
                }
#endif
                return entries;
            }

            // Struct used in building entries for the warning state map during compilation.
            // Entries sort by position in the tree.
            private struct WarningStateMapBuilderEntry : IComparable<WarningStateMapBuilderEntry>
            {
                // 0-based position in this tree
                public readonly int Position;

                // Id of the user diagnostic for which the state is being changed
                public readonly string Id;

                // The state to change the reporting for the specified diagnostic to
                public readonly ReportDiagnostic SpecificWarningOption;

                public WarningStateMapBuilderEntry(int position, string id, ReportDiagnostic specific)
                {
                    this.Position = position;
                    this.Id = id;
                    this.SpecificWarningOption = specific;
                }

                public int CompareTo(WarningStateMapBuilderEntry other)
                {
                    return this.Position - other.Position;
                }
            }
        }
    }
}
