﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class StateTableTests
    {
        [Fact]
        public void Node_Table_Entries_Can_Be_Enumerated()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(3), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added), (2, EntryState.Added), (3, EntryState.Added));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Table_Entries_Are_Flattened_When_Enumerated()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder();
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

            var builder = NodeStateTable<object>.Empty.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(o, o, o), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((o, EntryState.Added), (o, EntryState.Added), (o, EntryState.Added));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Table_Entries_Can_Be_Null()
        {
            object? o = new object();

            var builder = NodeStateTable<object?>.Empty.ToBuilder();
            builder.AddEntry(o, EntryState.Added);
            builder.AddEntry(null, EntryState.Added);
            builder.AddEntry(o, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((o, EntryState.Added), (null, EntryState.Added), (o, EntryState.Added));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Builder_Can_Add_Entries_From_Previous_Table()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2, 3), EntryState.Cached);
            builder.AddEntries(ImmutableArray.Create(4, 5), EntryState.Modified);
            builder.AddEntries(ImmutableArray.Create(6), EntryState.Added);
            var previousTable = builder.ToImmutableAndFree();

            builder = previousTable.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(10, 11), EntryState.Added);
            builder.TryUseCachedEntries(); // ((2, EntryState.Cached), (3, EntryState.Cached))
            builder.AddEntries(ImmutableArray.Create(20, 21, 22), EntryState.Modified);
            builder.RemoveEntries(); //((6, EntryState.Removed))); 
            var newTable = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((10, EntryState.Added), (11, EntryState.Added), (2, EntryState.Cached), (3, EntryState.Cached), (20, EntryState.Modified), (21, EntryState.Modified), (22, EntryState.Modified), (6, EntryState.Removed));
            AssertTableEntries(newTable, expected);
        }

        [Fact]
        public void Node_Table_Entries_Are_Cached_Or_Dropped_When_Cached()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4, 5, 6), EntryState.Removed);
            builder.AddEntries(ImmutableArray.Create(7, 8, 9), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added), (2, EntryState.Added), (3, EntryState.Added), (4, EntryState.Removed), (5, EntryState.Removed), (6, EntryState.Removed), (7, EntryState.Added), (8, EntryState.Added), (9, EntryState.Added));
            AssertTableEntries(table, expected);

            var compactedTable = table.AsCached();
            expected = ImmutableArray.Create((1, EntryState.Cached), (2, EntryState.Cached), (3, EntryState.Cached), (7, EntryState.Cached), (8, EntryState.Cached), (9, EntryState.Cached));
            AssertTableEntries(compactedTable, expected);
        }

        [Fact]
        public void Node_Table_AsCached_Occurs_Only_Once()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4, 5, 6), EntryState.Removed);
            builder.AddEntries(ImmutableArray.Create(7, 8, 9), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added), (2, EntryState.Added), (3, EntryState.Added), (4, EntryState.Removed), (5, EntryState.Removed), (6, EntryState.Removed), (7, EntryState.Added), (8, EntryState.Added), (9, EntryState.Added));
            AssertTableEntries(table, expected);

            var compactedTable = table.AsCached();
            expected = ImmutableArray.Create((1, EntryState.Cached), (2, EntryState.Cached), (3, EntryState.Cached), (7, EntryState.Cached), (8, EntryState.Cached), (9, EntryState.Cached));
            AssertTableEntries(compactedTable, expected);

            // calling as cached a second time just returns the same instance
            var compactedTable2 = compactedTable.AsCached();
            Assert.Same(compactedTable, compactedTable2);
        }

        [Fact]
        public void Node_Table_Single_Returns_First_Item()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            Assert.Equal(1, table.Single());
        }

        [Fact]
        public void Node_Table_Single_Returns_Second_Item_When_First_Is_Removed()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder();
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added);
            var table = builder.ToImmutableAndFree();

            AssertTableEntries(table, new[] { (1, EntryState.Added) });

            // remove the first item and replace it in the table
            builder = table.ToBuilder();
            builder.RemoveEntries();
            builder.AddEntries(ImmutableArray.Create(2), EntryState.Added);
            table = builder.ToImmutableAndFree();

            AssertTableEntries(table, new[] { (1, EntryState.Removed), (2, EntryState.Added) });
            Assert.Equal(2, table.Single());
        }

        [Fact]
        public void Driver_Table_Calls_Into_Node_With_Self()
        {
            DriverStateTable.Builder? passedIn = null;
            CallbackNode<int> callbackNode = new CallbackNode<int>((b, s) =>
            {
                passedIn = b;
                return s;
            });

            DriverStateTable.Builder builder = GetBuilder(DriverStateTable.Empty);
            builder.GetLatestStateTableForNode(callbackNode);

            Assert.Same(builder, passedIn);
        }

        [Fact]
        public void Driver_Table_Calls_Into_Node_With_EmptyState_FirstTime()
        {
            NodeStateTable<int>? passedIn = null;
            CallbackNode<int> callbackNode = new CallbackNode<int>((b, s) =>
            {
                passedIn = s;
                return s;
            });

            DriverStateTable.Builder builder = GetBuilder(DriverStateTable.Empty);
            builder.GetLatestStateTableForNode(callbackNode);

            Assert.Same(NodeStateTable<int>.Empty, passedIn);
        }

        [Fact]
        public void Driver_Table_Calls_Into_Node_With_PreviousTable()
        {
            var nodeBuilder = NodeStateTable<int>.Empty.ToBuilder();
            nodeBuilder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Cached);
            var newTable = nodeBuilder.ToImmutableAndFree();

            NodeStateTable<int>? passedIn = null;
            CallbackNode<int> callbackNode = new CallbackNode<int>((b, s) =>
            {
                passedIn = s;
                return newTable;
            });

            // empty first time
            DriverStateTable.Builder builder = GetBuilder(DriverStateTable.Empty);
            builder.GetLatestStateTableForNode(callbackNode);

            Assert.Same(NodeStateTable<int>.Empty, passedIn);

            // gives the returned table the second time around
            DriverStateTable.Builder builder2 = GetBuilder(builder.ToImmutable());
            builder2.GetLatestStateTableForNode(callbackNode);

            Assert.NotNull(passedIn);
            AssertTableEntries(passedIn!, new[] { (1, EntryState.Cached), (2, EntryState.Cached), (3, EntryState.Cached) });
        }

        [Fact]
        public void Driver_Table_Compacts_State_Tables_When_Made_Immutable()
        {
            var nodeBuilder = NodeStateTable<int>.Empty.ToBuilder();
            nodeBuilder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added);
            nodeBuilder.AddEntries(ImmutableArray.Create(4), EntryState.Removed);
            nodeBuilder.AddEntries(ImmutableArray.Create(5, 6), EntryState.Modified);

            var newTable = nodeBuilder.ToImmutableAndFree();

            NodeStateTable<int>? passedIn = null;
            CallbackNode<int> callbackNode = new CallbackNode<int>((b, s) =>
            {
                passedIn = s;
                return newTable;
            });

            // empty first time
            DriverStateTable.Builder builder = GetBuilder(DriverStateTable.Empty);
            builder.GetLatestStateTableForNode(callbackNode);
            Assert.Same(NodeStateTable<int>.Empty, passedIn);

            // gives the returned table the second time around
            DriverStateTable.Builder builder2 = GetBuilder(builder.ToImmutable());
            builder2.GetLatestStateTableForNode(callbackNode);

            // table returned from the first instance was compacted by the builder
            Assert.NotNull(passedIn);
            AssertTableEntries(passedIn!, new[] { (1, EntryState.Cached), (2, EntryState.Cached), (3, EntryState.Cached), (5, EntryState.Cached), (6, EntryState.Cached) });
        }

        [Fact]
        public void Driver_Table_Builder_Doesnt_Build_Twice()
        {
            int callCount = 0;
            CallbackNode<int> callbackNode = new CallbackNode<int>((b, s) =>
            {
                callCount++;
                return s;
            });

            // multiple gets will only call it once
            DriverStateTable.Builder builder = GetBuilder(DriverStateTable.Empty);
            builder.GetLatestStateTableForNode(callbackNode);
            builder.GetLatestStateTableForNode(callbackNode);
            builder.GetLatestStateTableForNode(callbackNode);

            Assert.Equal(1, callCount);

            // second time around we'll call it once, but no more
            DriverStateTable.Builder builder2 = GetBuilder(builder.ToImmutable());
            builder2.GetLatestStateTableForNode(callbackNode);
            builder2.GetLatestStateTableForNode(callbackNode);
            builder2.GetLatestStateTableForNode(callbackNode);

            Assert.Equal(2, callCount);
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

        private DriverStateTable.Builder GetBuilder(DriverStateTable previous)
        {
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);
            var c = CSharpCompilation.Create("empty");
            var state = new GeneratorDriverState(options,
                    CompilerAnalyzerConfigOptionsProvider.Empty,
                    ImmutableArray<ISourceGenerator>.Empty,
                    ImmutableArray<IIncrementalGenerator>.Empty,
                    ImmutableArray<AdditionalText>.Empty,
                    ImmutableArray<GeneratorState>.Empty,
                    previous,
                    enableIncremental: true);

            return new DriverStateTable.Builder(c, state, ImmutableArray<ISyntaxInputNode>.Empty);
        }

        private class CallbackNode<T> : IIncrementalGeneratorNode<T>
        {
            private readonly Func<DriverStateTable.Builder, NodeStateTable<T>, NodeStateTable<T>> _callback;

            public CallbackNode(Func<DriverStateTable.Builder, NodeStateTable<T>, NodeStateTable<T>> callback)
            {
                _callback = callback;
            }

            public NodeStateTable<T> UpdateStateTable(DriverStateTable.Builder graphState, NodeStateTable<T> previousTable, CancellationToken cancellationToken)
            {
                return _callback(graphState, previousTable);
            }

            public IIncrementalGeneratorNode<T> WithComparer(IEqualityComparer<T> comparer) => this;

            public void RegisterOutput(IIncrementalGeneratorOutputNode output) { }
        }
    }
}
