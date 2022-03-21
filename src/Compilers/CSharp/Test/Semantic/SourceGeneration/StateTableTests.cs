// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Test.Utilities;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class StateTableTests
    {
        [Fact]
        public void Node_Table_Entries_Can_Be_Enumerated()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(3), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added, 0), (2, EntryState.Added, 0), (3, EntryState.Added, 0));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Table_Entries_Are_Flattened_When_Enumerated()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4, 5, 6), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(7, 8, 9), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added, 0), (2, EntryState.Added, 1), (3, EntryState.Added, 2), (4, EntryState.Added, 0), (5, EntryState.Added, 1), (6, EntryState.Added, 2), (7, EntryState.Added, 0), (8, EntryState.Added, 1), (9, EntryState.Added, 2));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Table_Entries_Can_Be_The_Same_Object()
        {
            var o = new object();

            var builder = NodeStateTable<object>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(o, o, o), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((o, EntryState.Added, 0), (o, EntryState.Added, 1), (o, EntryState.Added, 2));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Table_Entries_Can_Be_Null()
        {
            object? o = new object();

            var builder = NodeStateTable<object?>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntry(o, EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntry(null, EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntry(o, EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((o, EntryState.Added, 0), (null, EntryState.Added, 0), (o, EntryState.Added, 0));
            AssertTableEntries(table, expected);
        }

        [Fact]
        public void Node_Builder_Can_Add_Entries_From_Previous_Table()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2, 3), EntryState.Cached, TimeSpan.Zero, default, EntryState.Cached);
            builder.AddEntries(ImmutableArray.Create(4, 5), EntryState.Modified, TimeSpan.Zero, default, EntryState.Modified);
            builder.AddEntries(ImmutableArray.Create(6), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var previousTable = builder.ToImmutableAndFree();

            builder = previousTable.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(10, 11), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.TryUseCachedEntries(TimeSpan.Zero, default, out var cachedEntries); // ((2, EntryState.Cached), (3, EntryState.Cached))
            builder.AddEntries(ImmutableArray.Create(20, 21, 22), EntryState.Modified, TimeSpan.Zero, default, EntryState.Modified);
            bool didRemoveEntries = builder.TryRemoveEntries(TimeSpan.Zero, default, out var removedEntries); //((6, EntryState.Removed))
            var newTable = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((10, EntryState.Added, 0), (11, EntryState.Added, 1), (2, EntryState.Cached, 0), (3, EntryState.Cached, 1), (20, EntryState.Modified, 0), (21, EntryState.Modified, 1), (22, EntryState.Modified, 2), (6, EntryState.Removed, 0));
            AssertTableEntries(newTable, expected);
            Assert.Equal(new[] { 2, 3 }, cachedEntries);
            Assert.Equal(6, Assert.Single(removedEntries));
            Assert.True(didRemoveEntries);
        }

        [Fact]
        public void Node_Table_Entries_Are_Cached_Or_Dropped_When_Cached()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4, 5, 6), EntryState.Removed, TimeSpan.Zero, default, EntryState.Removed);
            builder.AddEntries(ImmutableArray.Create(7, 8, 9), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added, 0), (2, EntryState.Added, 1), (3, EntryState.Added, 2), (4, EntryState.Removed, 0), (5, EntryState.Removed, 1), (6, EntryState.Removed, 2), (7, EntryState.Added, 0), (8, EntryState.Added, 1), (9, EntryState.Added, 2));
            AssertTableEntries(table, expected);

            var compactedTable = table.AsCached();
            expected = ImmutableArray.Create((1, EntryState.Cached, 0), (2, EntryState.Cached, 1), (3, EntryState.Cached, 2), (7, EntryState.Cached, 0), (8, EntryState.Cached, 1), (9, EntryState.Cached, 2));
            AssertTableEntries(compactedTable, expected);
        }

        [Fact]
        public void Node_Table_AsCached_Occurs_Only_Once()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4, 5, 6), EntryState.Removed, TimeSpan.Zero, default, EntryState.Removed);
            builder.AddEntries(ImmutableArray.Create(7, 8, 9), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added, 0), (2, EntryState.Added, 1), (3, EntryState.Added, 2), (4, EntryState.Removed, 0), (5, EntryState.Removed, 1), (6, EntryState.Removed, 2), (7, EntryState.Added, 0), (8, EntryState.Added, 1), (9, EntryState.Added, 2));
            AssertTableEntries(table, expected);

            var compactedTable = table.AsCached();
            expected = ImmutableArray.Create((1, EntryState.Cached, 0), (2, EntryState.Cached, 1), (3, EntryState.Cached, 2), (7, EntryState.Cached, 0), (8, EntryState.Cached, 1), (9, EntryState.Cached, 2));
            AssertTableEntries(compactedTable, expected);

            // calling as cached a second time just returns the same instance
            var compactedTable2 = compactedTable.AsCached();
            Assert.Same(compactedTable, compactedTable2);
        }

        [Fact]
        public void Node_Table_Single_Returns_First_Item()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            Assert.Equal(1, table.Single().item);
        }

        [Fact]
        public void Node_Table_Single_Returns_Second_Item_When_First_Is_Removed()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var table = builder.ToImmutableAndFree();

            AssertTableEntries(table, new[] { (1, EntryState.Added, 0) });

            // remove the first item and replace it in the table
            builder = table.ToBuilder(stepName: null, false);

            bool didRemoveEntries = builder.TryRemoveEntries(TimeSpan.Zero, default);
            Assert.True(didRemoveEntries);

            builder.AddEntries(ImmutableArray.Create(2), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            table = builder.ToImmutableAndFree();

            AssertTableEntries(table, new[] { (1, EntryState.Removed, 0), (2, EntryState.Added, 0) });
            Assert.Equal(2, table.Single().item);
        }

        [Fact]
        public void Node_Builder_Handles_Modification_When_Both_Tables_Have_Empty_Entries()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1, 2), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray<int>.Empty, EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(3, 4), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var previousTable = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added, 0), (2, EntryState.Added, 1), (3, EntryState.Added, 0), (4, EntryState.Added, 1));
            AssertTableEntries(previousTable, expected);

            builder = previousTable.ToBuilder(stepName: null, false);
            Assert.True(builder.TryModifyEntries(ImmutableArray.Create(3, 2), EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));
            Assert.True(builder.TryModifyEntries(ImmutableArray<int>.Empty, EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));
            Assert.True(builder.TryModifyEntries(ImmutableArray.Create(3, 5), EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));

            var newTable = builder.ToImmutableAndFree();

            expected = ImmutableArray.Create((3, EntryState.Modified, 0), (2, EntryState.Cached, 1), (3, EntryState.Cached, 0), (5, EntryState.Modified, 1));
            AssertTableEntries(newTable, expected);
        }

        [Fact]
        public void Node_Table_Doesnt_Modify_Single_Item_Multiple_Times_When_Same()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(3), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(4), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var previousTable = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added, 0), (2, EntryState.Added, 0), (3, EntryState.Added, 0), (4, EntryState.Added, 0));
            AssertTableEntries(previousTable, expected);

            builder = previousTable.ToBuilder(stepName: null, false);
            Assert.True(builder.TryModifyEntry(1, EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));
            Assert.True(builder.TryModifyEntry(2, EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));
            Assert.True(builder.TryModifyEntry(5, EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));
            Assert.True(builder.TryModifyEntry(4, EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));

            var newTable = builder.ToImmutableAndFree();

            expected = ImmutableArray.Create((1, EntryState.Cached, 0), (2, EntryState.Cached, 0), (5, EntryState.Modified, 0), (4, EntryState.Cached, 0));
            AssertTableEntries(newTable, expected);
        }

        [Fact]
        public void Node_Table_Caches_Previous_Object_When_Modification_Considered_Cached()
        {
            var builder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            builder.AddEntries(ImmutableArray.Create(1), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(2), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            builder.AddEntries(ImmutableArray.Create(3), EntryState.Added, TimeSpan.Zero, default, EntryState.Added);
            var previousTable = builder.ToImmutableAndFree();

            var expected = ImmutableArray.Create((1, EntryState.Added, 0), (2, EntryState.Added, 0), (3, EntryState.Added, 0));
            AssertTableEntries(previousTable, expected);

            builder = previousTable.ToBuilder(stepName: null, false);
            Assert.True(builder.TryModifyEntry(1, EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));           // ((1, EntryState.Cached))
            Assert.True(builder.TryModifyEntry(4, EqualityComparer<int>.Default, TimeSpan.Zero, default, EntryState.Modified));           // ((4, EntryState.Modified))
            Assert.True(builder.TryModifyEntry(5, new LambdaComparer<int>((i, j) => true), TimeSpan.Zero, default, EntryState.Modified)); // ((3, EntryState.Cached))
            var newTable = builder.ToImmutableAndFree();

            expected = ImmutableArray.Create((1, EntryState.Cached, 0), (4, EntryState.Modified, 0), (3, EntryState.Cached, 0));
            AssertTableEntries(newTable, expected);
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
            var nodeBuilder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, false);
            nodeBuilder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Cached, TimeSpan.Zero, default, EntryState.Cached);
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
            AssertTableEntries(passedIn!, new[] { (1, EntryState.Cached, 0), (2, EntryState.Cached, 1), (3, EntryState.Cached, 2) });
        }

        [Fact]
        public void Driver_Table_Compacts_State_Tables_And_Drops_Steps_When_Made_Immutable()
        {
            var nodeBuilder = NodeStateTable<int>.Empty.ToBuilder(stepName: null, true);
            var emptyInputSteps = ImmutableArray<(IncrementalGeneratorRunStep InputStep, int OutputIndex)>.Empty;
            nodeBuilder.AddEntries(ImmutableArray.Create(1, 2, 3), EntryState.Added, TimeSpan.Zero, emptyInputSteps, EntryState.Added);
            nodeBuilder.AddEntries(ImmutableArray.Create(4), EntryState.Removed, TimeSpan.Zero, emptyInputSteps, EntryState.Removed);
            nodeBuilder.AddEntries(ImmutableArray.Create(5, 6), EntryState.Modified, TimeSpan.Zero, emptyInputSteps, EntryState.Modified);

            var newTable = nodeBuilder.ToImmutableAndFree();

            Assert.True(newTable.HasTrackedSteps);
            Assert.Equal(3, newTable.Steps.Length);

            NodeStateTable<int>? passedIn = null;
            CallbackNode<int> callbackNode = new CallbackNode<int>((b, s) =>
            {
                passedIn = s;
                return newTable;
            });

            // empty first time
            DriverStateTable.Builder builder = GetBuilder(DriverStateTable.Empty, trackIncrementalGeneratorSteps: true);
            builder.GetLatestStateTableForNode(callbackNode);
            Assert.Same(NodeStateTable<int>.Empty, passedIn);

            // gives the returned table the second time around
            DriverStateTable driverStateTable = builder.ToImmutable();
            DriverStateTable.Builder builder2 = GetBuilder(driverStateTable, trackIncrementalGeneratorSteps: true);
            builder2.GetLatestStateTableForNode(callbackNode);

            // table returned from the first instance was compacted by the builder
            Assert.NotNull(passedIn);
            AssertTableEntries(passedIn!, new[] { (1, EntryState.Cached, 0), (2, EntryState.Cached, 1), (3, EntryState.Cached, 2), (5, EntryState.Cached, 0), (6, EntryState.Cached, 1) });
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

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Batch_Node_Records_NewInput_Step_On_First_Run()
        {
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1, 2, 3));
            BatchNode<int> batchNode = new BatchNode<int>(inputNode, name: "Batch");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty, true);
            var table = dstBuilder.GetLatestStateTableForNode(batchNode);

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Equal("Batch", step.Name);
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        },
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(2, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        },
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(3, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(ImmutableArray.Create(1, 2, 3), (IEnumerable<int>)output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });
        }

        [Fact]
        public void Batch_Node_Is_Cached_If_All_Inputs_Are_Cached()
        {
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1, 2, 3));
            BatchNode<int> batchNode = new BatchNode<int>(inputNode);

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(batchNode);

            // second time through should show as cached
            dstBuilder = GetBuilder(dstBuilder.ToImmutable());
            var table = dstBuilder.GetLatestStateTableForNode(batchNode);

            AssertTableEntries(table, new[] { (ImmutableArray.Create(1, 2, 3), EntryState.Cached, 0) });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Batch_Node_Records_Cached_Step_If_All_Inputs_Are_Cached()
        {
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1, 2, 3));
            BatchNode<int> batchNode = new BatchNode<int>(inputNode, name: "Batch");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(batchNode);

            // second time through should show as cached
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(batchNode);

            var step = Assert.Single(table.Steps);

            Assert.Equal("Batch", step.Name);
            Assert.Collection(step.Inputs,
                source =>
                {
                    Assert.Equal(0, source.OutputIndex);
                    Assert.Equal(1, source.Source.Outputs[source.OutputIndex].Value);
                    Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                },
                source =>
                {
                    Assert.Equal(0, source.OutputIndex);
                    Assert.Equal(2, source.Source.Outputs[source.OutputIndex].Value);
                    Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                },
                source =>
                {
                    Assert.Equal(0, source.OutputIndex);
                    Assert.Equal(3, source.Source.Outputs[source.OutputIndex].Value);
                    Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                });
            Assert.Collection(step.Outputs,
                output =>
                {
                    Assert.Equal(ImmutableArray.Create(1, 2, 3), (IEnumerable<int>)output.Value);
                    Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Batch_Node_Steps_Records_Removed_Steps_As_Inputs()
        {
            var inputValue = ImmutableArray.Create(1, 2, 3);
            var inputNode = new InputNode<int>((_) => inputValue).WithTrackingName("Input");
            BatchNode<int> batchNode = new BatchNode<int>(inputNode, name: "Batch");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(batchNode);

            // removal of item for second call will be recorded in the steps.
            inputValue = ImmutableArray.Create(1, 2);
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(batchNode);

            var step = Assert.Single(table.Steps);

            Assert.Equal("Batch", step.Name);
            Assert.Collection(step.Inputs,
                source =>
                {
                    Assert.Equal(0, source.OutputIndex);
                    Assert.Equal(1, source.Source.Outputs[source.OutputIndex].Value);
                    Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                },
                source =>
                {
                    Assert.Equal(0, source.OutputIndex);
                    Assert.Equal(2, source.Source.Outputs[source.OutputIndex].Value);
                    Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                },
                source =>
                {
                    Assert.Equal(0, source.OutputIndex);
                    Assert.Equal(3, source.Source.Outputs[source.OutputIndex].Value);
                    Assert.Equal(IncrementalStepRunReason.Removed, source.Source.Outputs[source.OutputIndex].Reason);
                });
            Assert.Collection(step.Outputs,
                output =>
                {
                    Assert.Equal(ImmutableArray.Create(1, 2), (IEnumerable<int>)output.Value);
                    Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                });
        }

        [Fact]
        public void Batch_Node_Is_Not_Cached_When_Inputs_Are_Changed()
        {
            int thirdElement = 3;

            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1, 2, thirdElement++));
            BatchNode<int> batchNode = new BatchNode<int>(inputNode);

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(batchNode);

            // second time through should show as modified
            dstBuilder = GetBuilder(dstBuilder.ToImmutable());
            var table = dstBuilder.GetLatestStateTableForNode(batchNode);

            AssertTableEntries(table, new[] { (ImmutableArray.Create(1, 2, 4), EntryState.Modified, 0) });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Batch_Node_Records_InputModified_Step_When_Inputs_Are_Changed()
        {
            int thirdElement = 3;
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1, 2, thirdElement));
            BatchNode<int> batchNode = new BatchNode<int>(inputNode, name: "Batch");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(batchNode);

            thirdElement = 4;
            // second time through should show as modified
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(batchNode);

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Equal("Batch", step.Name);
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        },
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(2, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        },
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(4, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(ImmutableArray.Create(1, 2, 4), (IEnumerable<int>)output.Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                        });
                });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Transform_Node_Records_NewInput_OnFirst_Run()
        {
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1));
            TransformNode<int, int> transformNode = new TransformNode<int, int>(inputNode, (i, ct) => i, name: "Transform");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty, true);
            var table = dstBuilder.GetLatestStateTableForNode(transformNode);

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Equal("Transform", step.Name);
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.New, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(1, output.Value);
                            Assert.Equal(IncrementalStepRunReason.New, output.Reason);
                        });
                });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Transform_Node_Records_InputCached_When_Input_Is_Cached()
        {
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1));
            TransformNode<int, int> transformNode = new TransformNode<int, int>(inputNode, (i, ct) => i, name: "Transform");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(transformNode);

            // second time through should show as cached
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(transformNode);

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Equal("Transform", step.Name);
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(1, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(1, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Cached, output.Reason);
                        });
                });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Transform_Node_Records_InputModified_When_Input_Is_Modified()
        {
            int value = 1;
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(value));
            TransformNode<int, int> transformNode = new TransformNode<int, int>(inputNode, (i, ct) => i, name: "Transform");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(transformNode);

            value = 13;

            // second time through should show as modified
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(transformNode);

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Equal("Transform", step.Name);
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(value, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(value, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, output.Reason);
                        });
                });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void Transform_Node_Records_InputModified_OutputUnchanged_When_Input_Is_Modified_Output_Is_Cached()
        {
            int value = 1;
            int transformNodeResult = 20;
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(value));
            TransformNode<int, int> transformNode = new TransformNode<int, int>(inputNode, (i, ct) => transformNodeResult, name: "Transform");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(transformNode);

            value = 13;

            // second time through should show as modified
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(transformNode);

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Equal("Transform", step.Name);
                    Assert.Collection(step.Inputs,
                        source =>
                        {
                            Assert.Equal(0, source.OutputIndex);
                            Assert.Equal(value, source.Source.Outputs[source.OutputIndex].Value);
                            Assert.Equal(IncrementalStepRunReason.Modified, source.Source.Outputs[source.OutputIndex].Reason);
                        });
                    Assert.Collection(step.Outputs,
                        output =>
                        {
                            Assert.Equal(transformNodeResult, output.Value);
                            Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason);
                        });
                });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void InputNode_With_Different_Element_Count_Records_Add_Remove_For_Replaced_Items()
        {
            ImmutableArray<int> inputNodeValue = ImmutableArray.Create(1, 2, 3);
            var inputNode = new InputNode<int>((_) => inputNodeValue).WithTrackingName("TestStep");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(inputNode);

            // Create a new set of input values that differs in length.
            inputNodeValue = ImmutableArray.Create(1, 4, 5, 6);

            // second time through should show any repeating elements as cached and any other elements as added/removed.
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(inputNode);

            AssertTableEntries(table, new[] { (1, EntryState.Cached, 0), (2, EntryState.Removed, 0), (3, EntryState.Removed, 0), (4, EntryState.Added, 0), (5, EntryState.Added, 0), (6, EntryState.Added, 0) });

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Empty(step.Inputs);
                    Assert.Equal((1, IncrementalStepRunReason.Cached), Assert.Single(step.Outputs));
                },
                step =>
                {
                    Assert.Empty(step.Inputs);
                    Assert.Equal((2, IncrementalStepRunReason.Removed), Assert.Single(step.Outputs));
                },
                step =>
                {
                    Assert.Empty(step.Inputs);
                    Assert.Equal((3, IncrementalStepRunReason.Removed), Assert.Single(step.Outputs));
                },
                step =>
                {
                    Assert.Empty(step.Inputs);
                    Assert.Equal((4, IncrementalStepRunReason.New), Assert.Single(step.Outputs));
                },
                step =>
                {
                    Assert.Empty(step.Inputs);
                    Assert.Equal((5, IncrementalStepRunReason.New), Assert.Single(step.Outputs));
                },
                step =>
                {
                    Assert.Empty(step.Inputs);
                    Assert.Equal((6, IncrementalStepRunReason.New), Assert.Single(step.Outputs));
                });
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void TransformNode_Records_Removed_Outputs_Of_Removed_Inputs()
        {
            ImmutableArray<int> inputNodeValue = ImmutableArray.Create(1, 2, 3);
            var inputNode = new InputNode<int>((_) => inputNodeValue);
            var transformNode = new TransformNode<int, int>(inputNode, (i, ct) => ImmutableArray.Create(i)).WithTrackingName("TestStep");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(transformNode);

            // Create a new set of input values that differs in length to force add/remove semantics.
            inputNodeValue = ImmutableArray.Create(1, 4, 5, 6);

            // second time through should show any repeating elements as cached and any other elements as added/removed.
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(transformNode);

            Assert.Collection(table.Steps,
                step =>
                    Assert.Collection(step.Outputs, output => Assert.Equal((1, IncrementalStepRunReason.Cached), output)),
                step =>
                    Assert.Collection(step.Outputs, output => Assert.Equal((2, IncrementalStepRunReason.Removed), output)),
                step =>
                    Assert.Collection(step.Outputs, output => Assert.Equal((3, IncrementalStepRunReason.Removed), output)),
                step =>
                    Assert.Collection(step.Outputs, output => Assert.Equal((4, IncrementalStepRunReason.New), output)),
                step =>
                    Assert.Collection(step.Outputs, output => Assert.Equal((5, IncrementalStepRunReason.New), output)),
                step =>
                    Assert.Collection(step.Outputs, output => Assert.Equal((6, IncrementalStepRunReason.New), output)));
        }

        [Fact]
        [WorkItem(54832, "https://github.com/dotnet/roslyn/issues/54832")]
        public void CombineNode_Records_Removed_Outputs_Of_Removed_First_Input()
        {
            ImmutableArray<int> inputNodeValue = ImmutableArray.Create(1, 2, 3);
            var inputNode = new InputNode<int>((_) => inputNodeValue);
            var input2Node = new InputNode<int>((_) => ImmutableArray.Create(0));
            var combineNode = new CombineNode<int, int>(inputNode, input2Node).WithTrackingName("TestStep");

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(combineNode);

            // Create a new set of input values that differs in length to force add/remove semantics.
            inputNodeValue = ImmutableArray.Create(1, 4, 5, 6);

            // second time through should show any repeating elements as cached and any other elements as added/removed.
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), true);
            var table = dstBuilder.GetLatestStateTableForNode(combineNode);

            Assert.Collection(table.Steps,
                step =>
                {
                    Assert.Equal("TestStep", step.Name);
                    Assert.Equal(IncrementalStepRunReason.Cached, step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason);
                    Assert.Collection(step.Outputs, output => Assert.Equal(((1, 0), IncrementalStepRunReason.Cached), output));
                },
                step =>
                {
                    Assert.Equal("TestStep", step.Name);
                    Assert.Equal(IncrementalStepRunReason.Removed, step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason);
                    Assert.Collection(step.Outputs, output => Assert.Equal(((2, 0), IncrementalStepRunReason.Removed), output));
                },
                step =>
                {
                    Assert.Equal("TestStep", step.Name);
                    Assert.Equal(IncrementalStepRunReason.Removed, step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason);
                    Assert.Collection(step.Outputs, output => Assert.Equal(((3, 0), IncrementalStepRunReason.Removed), output));
                },
                step =>
                {
                    Assert.Equal("TestStep", step.Name);
                    Assert.Equal(IncrementalStepRunReason.New, step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason);
                    Assert.Collection(step.Outputs, output => Assert.Equal(((4, 0), IncrementalStepRunReason.New), output));
                },
                step =>
                {
                    Assert.Equal("TestStep", step.Name);
                    Assert.Equal(IncrementalStepRunReason.New, step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason);
                    Assert.Collection(step.Outputs, output => Assert.Equal(((5, 0), IncrementalStepRunReason.New), output));
                },
                step =>
                {
                    Assert.Equal("TestStep", step.Name);
                    Assert.Equal(IncrementalStepRunReason.New, step.Inputs[0].Source.Outputs[step.Inputs[0].OutputIndex].Reason);
                    Assert.Collection(step.Outputs, output => Assert.Equal(((6, 0), IncrementalStepRunReason.New), output));
                });
        }

        [Fact]
        public void User_Comparer_Is_Not_Used_To_Determine_Inputs()
        {
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1, 2, 3))
                                .WithComparer(new LambdaComparer<int>((a, b) => false));

            // first time through will always be added (because it's not been run before)
            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty);
            _ = dstBuilder.GetLatestStateTableForNode(inputNode);

            // second time through should show as cached, even though we supplied a comparer (comparer should only used to turn modified => cached)
            dstBuilder = GetBuilder(dstBuilder.ToImmutable());
            var table = dstBuilder.GetLatestStateTableForNode(inputNode);

            AssertTableEntries(table, new[] { (1, EntryState.Cached, 0), new(2, EntryState.Cached, 0), new(3, EntryState.Cached, 0) });
        }

        [Fact]
        public void RecordedStep_Tree_Includes_Most_Recent_Recording_Of_Run_Even_When_All_Inputs_Cached()
        {
            int thirdValue = 3;
            var inputNode = new InputNode<int>((_) => ImmutableArray.Create(1, 2, thirdValue)).WithTrackingName("Input");
            var batchNode = new BatchNode<int>(inputNode, name: "Batch");
            var transformNode = new TransformNode<ImmutableArray<int>, int>(batchNode, (arr, ct) => arr, name: "Transform");
            var filterNode = new TransformNode<int, int>(transformNode, (i, ct) => i <= 2 ? ImmutableArray.Create(i) : ImmutableArray<int>.Empty, name: "Filter");
            var doubleNode = new TransformNode<int, int>(filterNode, (i, ct) => i * 2, name: "Double");
            var addOneNode = new TransformNode<int, int>(doubleNode, (i, ct) => i + 1, name: "AddOne");

            DriverStateTable.Builder dstBuilder = GetBuilder(DriverStateTable.Empty, trackIncrementalGeneratorSteps: true);

            List<IncrementalGeneratorRunStep> steps = new();

            _ = dstBuilder.GetLatestStateTableForNode(addOneNode);

            thirdValue = 4;

            // second time through we should be able to see that the third input value was 4 when getting to the batch node through tree traversal.
            dstBuilder = GetBuilder(dstBuilder.ToImmutable(), trackIncrementalGeneratorSteps: true);
            var table = dstBuilder.GetLatestStateTableForNode(addOneNode);

            var addOneStep = table.Steps[0];

            var doubleStep = addOneStep.Inputs[0].Source;

            var filterNodeStep = doubleStep.Inputs[0].Source;

            var transformNodeStep = filterNodeStep.Inputs[0].Source;

            Assert.Equal(thirdValue, (int)transformNodeStep.Outputs[2].Value);
        }

        private void AssertTableEntries<T>(NodeStateTable<T> table, IList<(T Item, EntryState State, int OutputIndex)> expected)
        {
            int index = 0;
            foreach (var entry in table)
            {
                Assert.Equal(expected[index].Item, entry.Item);
                Assert.Equal(expected[index].State, entry.State);
                Assert.Equal(expected[index].OutputIndex, entry.OutputIndex);
                index++;
            }
        }

        private void AssertTableEntries<T>(NodeStateTable<ImmutableArray<T>> table, IList<(ImmutableArray<T> Item, EntryState State, int OutputIndex)> expected)
        {
            int index = 0;
            foreach (var entry in table)
            {
                AssertEx.Equal(expected[index].Item, entry.Item);
                Assert.Equal(expected[index].State, entry.State);
                Assert.Equal(expected[index].OutputIndex, entry.OutputIndex);
                index++;
            }
        }

        private DriverStateTable.Builder GetBuilder(DriverStateTable previous, bool trackIncrementalGeneratorSteps = false)
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
                    SyntaxStore.Empty,
                    disabledOutputs: IncrementalGeneratorOutputKind.None,
                    runtime: TimeSpan.Zero,
                    trackIncrementalGeneratorSteps: trackIncrementalGeneratorSteps);

            return new DriverStateTable.Builder(c, state, SyntaxStore.Empty.ToBuilder(c, ImmutableArray<SyntaxInputNode>.Empty, trackIncrementalGeneratorSteps, cancellationToken: default));
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

            public IIncrementalGeneratorNode<T> WithTrackingName(string name) => this;

            public void RegisterOutput(IIncrementalGeneratorOutputNode output) { }
        }
    }
}
