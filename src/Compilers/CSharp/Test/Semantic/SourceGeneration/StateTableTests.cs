// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class StateTableTests
    {
        [Fact]
        public void Node_Table_Entries_Can_Be_Enumerated()
        {
            var builder = new NodeStateTable<int>.Builder();
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(3), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added), (2, EntryState.Added), (3, EntryState.Added));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Table_Entries_Are_Flattend_When_Enumerated()
        {
            var builder = new NodeStateTable<int>.Builder();
            builder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4, 5, 6), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(7, 8, 9), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added), (2, EntryState.Added), (3, EntryState.Added), (4, EntryState.Added), (5, EntryState.Added), (6, EntryState.Added), (7, EntryState.Added), (8, EntryState.Added), (9, EntryState.Added));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Table_Entries_Can_Be_The_Same_Object()
        {
            var o = new object();

            var builder = new NodeStateTable<object>.Builder();
            builder.AddEntries(ImmutableArray.Create(o, o, o), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((o, EntryState.Added), (o, EntryState.Added), (o, EntryState.Added));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Builder_Can_Add_Entries_From_Previous_Table()
        {
            var builder = new NodeStateTable<int>.Builder();
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2, 3), EntryState.Cached);
            builder.AddEntries(ImmutableArray.Create(4, 5), EntryState.Modified);
            builder.AddEntries(ImmutableArray.Create(6), EntryState.Added);
            var previousTable = builder.ToImmutableAndFree();

            builder = new NodeStateTable<int>.Builder();
            builder.AddEntries(ImmutableArray.Create(10, 11), EntryState.Added);
            builder.AddEntriesFromPreviousTable(previousTable, EntryState.Cached); // ((2, EntryState.Cached), (3, EntryState.Cached))
            builder.AddEntries(ImmutableArray.Create(20, 21, 22), EntryState.Modified);
            builder.AddEntriesFromPreviousTable(previousTable, EntryState.Removed); //((6, EntryState.Removed))); 
            var newTable = builder.ToImmutableAndFree();


            var expected = ImmutableArray.Create((10, EntryState.Added), (11, EntryState.Added), (2, EntryState.Cached), (3, EntryState.Cached), (20, EntryState.Modified), (21, EntryState.Modified), (22, EntryState.Modified), (6, EntryState.Removed));
            AssertTableEntries(newTable, expected);
        }

        [Fact]
        public void Node_Table_Entries_Are_Cached_Or_Removed_When_Compacted()
        {
            var builder = new NodeStateTable<int>.Builder();
            builder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4, 5, 6), EntryState.Removed);
            builder.AddEntries(ImmutableArray.Create(7, 8, 9), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added), (2, EntryState.Added), (3, EntryState.Added), (4, EntryState.Removed), (5, EntryState.Removed), (6, EntryState.Removed), (7, EntryState.Added), (8, EntryState.Added), (9, EntryState.Added));
            AssertTableEntries(table, expected);

            var compactedTable = (NodeStateTable<int>)table.Compact();
            expected = ImmutableArray.Create((1, EntryState.Cached), (2, EntryState.Cached), (3, EntryState.Cached), (7, EntryState.Cached), (8, EntryState.Cached), (9, EntryState.Cached));
            AssertTableEntries(compactedTable, expected);
        }

        [Fact]
        public void Driver_Table_Entries_Can_Be_Looked_Up()
        {

        }

        private void AssertTableEntries<T>(NodeStateTable<T> table, IList<(T item, EntryState state)> expected)
        {
            int index = 0;
            foreach (var entry in table)
            {
                Assert.Equal(expected[index].item, entry.item);
                Assert.Equal(expected[index].state, entry.state);
                index++;
            }
        }
    }
}
