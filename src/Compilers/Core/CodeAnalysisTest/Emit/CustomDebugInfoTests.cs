// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Emit
{
    public class CustomDebugInfoTests
    {
        [Fact]
        public void TryGetCustomDebugInfoRecord1()
        {
            byte[] cdi;

            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.TryGetCustomDebugInfoRecord(new byte[0], CustomDebugInfoKind.EditAndContinueLocalSlotMap));
            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.TryGetCustomDebugInfoRecord(new byte[] { 1 }, CustomDebugInfoKind.EditAndContinueLocalSlotMap));
            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.TryGetCustomDebugInfoRecord(new byte[] { 1, 2 }, CustomDebugInfoKind.EditAndContinueLocalSlotMap));

            // unknown version
            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(new byte[] { 5, 1, 0, 0 }, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

            // incomplete record header
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                4, (byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap,
            };

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

            // record size too small
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0, 0, 0, 0,
            };

            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap));

            // invalid record size = Int32.MinValue
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x00, 0x00, 0x00, 0x80,
                0, 0, 0, 0
            };

            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap));

            // empty record
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x08, 0x00, 0x00, 0x00,
            };

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsEmpty);

            // record size too big
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x0a, 0x00, 0x00, 0x00,
                0xab
            };

            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap));

            // valid record
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xab
            };

            AssertEx.Equal(new byte[] { 0xab }, CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap));

            // record not matching
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.DynamicLocals, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xab
            };

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

            // unknown record kind
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/0xff, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xab
            };

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

            // multiple records (number in global header is ignored, the first matching record is returned)
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xab,
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xcd
            };

            AssertEx.Equal(new byte[] { 0xab }, CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap));

            // multiple records (number in global header is ignored, the first record is returned)
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.DynamicLocals, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xab,
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xcd
            };

            AssertEx.Equal(new byte[] { 0xcd }, CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap));

            // multiple records (number in global header is ignored, the first record is returned)
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.DynamicLocals, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xab,
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x09, 0x00, 0x00, 0x00,
                0xcd
            };

            AssertEx.Equal(new byte[] { 0xab }, CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.DynamicLocals));
        }

        [Fact]
        public void UncompressSlotMap1()
        {
            using (new EnsureEnglishUICulture())
            {
                var e = Assert.Throws<InvalidDataException>(() => EditAndContinueMethodDebugInformation.Create(ImmutableArray.Create(new byte[] { 0x01, 0x68, 0xff }), ImmutableArray<byte>.Empty));
                Assert.Equal("Invalid data at offset 3: 01-68-FF*", e.Message);

                e = Assert.Throws<InvalidDataException>(() => EditAndContinueMethodDebugInformation.Create(ImmutableArray.Create(new byte[] { 0x01, 0x68, 0xff, 0xff, 0xff, 0xff }), ImmutableArray<byte>.Empty));
                Assert.Equal("Invalid data at offset 3: 01-68-FF*FF-FF-FF", e.Message);

                e = Assert.Throws<InvalidDataException>(() => EditAndContinueMethodDebugInformation.Create(ImmutableArray.Create(new byte[] { 0xff, 0xff, 0xff, 0xff }), ImmutableArray<byte>.Empty));
                Assert.Equal("Invalid data at offset 1: FF*FF-FF-FF", e.Message);

                byte[] largeData = new byte[10000];
                largeData[400] = 0xff;
                largeData[401] = 0xff;
                largeData[402] = 0xff;
                largeData[403] = 0xff;
                largeData[404] = 0xff;
                largeData[405] = 0xff;

                e = Assert.Throws<InvalidDataException>(() => EditAndContinueMethodDebugInformation.Create(ImmutableArray.Create(largeData), ImmutableArray<byte>.Empty));
                Assert.Equal(
                    "Invalid data at offset 401: 00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-FF*FF-FF-FF-FF-FF-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00...", e.Message);
            }
        }

        [Fact]
        public void EditAndContinueLocalSlotMap_NegativeSyntaxOffsets()
        {
            var slots = ImmutableArray.Create(
                new LocalSlotDebugInfo(SynthesizedLocalKind.UserDefined, new LocalDebugId(-1, 10)),
                new LocalSlotDebugInfo(SynthesizedLocalKind.TryAwaitPendingCaughtException, new LocalDebugId(-20000, 10)));

            var closures = ImmutableArray<ClosureDebugInfo>.Empty;
            var lambdas = ImmutableArray<LambdaDebugInfo>.Empty;
            var states = ImmutableArray<StateMachineStateDebugInfo>.Empty;

            var cmw = new BlobBuilder();

            new EditAndContinueMethodDebugInformation(123, slots, closures, lambdas, states).SerializeLocalSlots(cmw);

            var bytes = cmw.ToImmutableArray();
            AssertEx.Equal(new byte[] { 0xFF, 0xC0, 0x00, 0x4E, 0x20, 0x81, 0xC0, 0x00, 0x4E, 0x1F, 0x0A, 0x9A, 0x00, 0x0A }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(bytes, default(ImmutableArray<byte>)).LocalSlots;

            AssertEx.Equal(slots, deserialized);
        }

        [Fact]
        public void EditAndContinueLambdaAndClosureMap_NegativeSyntaxOffsets()
        {
            var slots = ImmutableArray<LocalSlotDebugInfo>.Empty;

            var closures = ImmutableArray.Create(
                new ClosureDebugInfo(-100, new DebugId(0, 0)),
                new ClosureDebugInfo(10, new DebugId(1, 0)),
                new ClosureDebugInfo(-200, new DebugId(2, 0)));

            var lambdas = ImmutableArray.Create(
                new LambdaDebugInfo(20, new DebugId(0, 0), 1),
                new LambdaDebugInfo(-50, new DebugId(1, 0), 0),
                new LambdaDebugInfo(-180, new DebugId(2, 0), LambdaDebugInfo.StaticClosureOrdinal));

            var states = ImmutableArray<StateMachineStateDebugInfo>.Empty;
            var cmw = new BlobBuilder();

            new EditAndContinueMethodDebugInformation(0x7b, slots, closures, lambdas, states).SerializeLambdaMap(cmw);

            var bytes = cmw.ToImmutableArray();

            AssertEx.Equal(new byte[] { 0x7C, 0x80, 0xC8, 0x03, 0x64, 0x80, 0xD2, 0x00, 0x80, 0xDC, 0x03, 0x80, 0x96, 0x02, 0x14, 0x01 }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(default(ImmutableArray<byte>), bytes);

            AssertEx.Equal(closures, deserialized.Closures);
            AssertEx.Equal(lambdas, deserialized.Lambdas);
        }

        [Fact]
        public void EditAndContinueLambdaAndClosureMap_NoClosures()
        {
            var slots = ImmutableArray<LocalSlotDebugInfo>.Empty;

            var closures = ImmutableArray<ClosureDebugInfo>.Empty;
            var lambdas = ImmutableArray.Create(new LambdaDebugInfo(20, new DebugId(0, 0), LambdaDebugInfo.StaticClosureOrdinal));
            var states = ImmutableArray<StateMachineStateDebugInfo>.Empty;

            var cmw = new BlobBuilder();

            new EditAndContinueMethodDebugInformation(-1, slots, closures, lambdas, states).SerializeLambdaMap(cmw);

            var bytes = cmw.ToImmutableArray();

            AssertEx.Equal(new byte[] { 0x00, 0x01, 0x00, 0x15, 0x01 }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(default(ImmutableArray<byte>), bytes);

            AssertEx.Equal(closures, deserialized.Closures);
            AssertEx.Equal(lambdas, deserialized.Lambdas);
        }

        [Fact]
        public void EditAndContinueLambdaAndClosureMap_NoLambdas()
        {
            // should not happen in practice, but EditAndContinueMethodDebugInformation should handle it just fine

            var slots = ImmutableArray<LocalSlotDebugInfo>.Empty;
            var closures = ImmutableArray<ClosureDebugInfo>.Empty;
            var lambdas = ImmutableArray<LambdaDebugInfo>.Empty;
            var states = ImmutableArray<StateMachineStateDebugInfo>.Empty;

            var cmw = new BlobBuilder();

            new EditAndContinueMethodDebugInformation(10, slots, closures, lambdas, states).SerializeLambdaMap(cmw);

            var bytes = cmw.ToImmutableArray();

            AssertEx.Equal(new byte[] { 0x0B, 0x01, 0x00 }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(default(ImmutableArray<byte>), bytes);

            AssertEx.Equal(closures, deserialized.Closures);
            AssertEx.Equal(lambdas, deserialized.Lambdas);
        }

        [Fact]
        public void StateMachineStateDebugInfo()
        {
            var cmw = new BlobBuilder();

            var info = new EditAndContinueMethodDebugInformation(
                methodOrdinal: 1,
                localSlots: ImmutableArray<LocalSlotDebugInfo>.Empty,
                closures: ImmutableArray<ClosureDebugInfo>.Empty,
                lambdas: ImmutableArray<LambdaDebugInfo>.Empty,
                stateMachineStates: ImmutableArray.Create(
                    new StateMachineStateDebugInfo(syntaxOffset: 0x10, new AwaitDebugId(2), (StateMachineState)0),
                    new StateMachineStateDebugInfo(syntaxOffset: 0x30, new AwaitDebugId(0), (StateMachineState)5),
                    new StateMachineStateDebugInfo(syntaxOffset: 0x10, new AwaitDebugId(0), (StateMachineState)1),
                    new StateMachineStateDebugInfo(syntaxOffset: 0x20, new AwaitDebugId(0), (StateMachineState)3),
                    new StateMachineStateDebugInfo(syntaxOffset: 0x10, new AwaitDebugId(1), (StateMachineState)2),
                    new StateMachineStateDebugInfo(syntaxOffset: 0x20, new AwaitDebugId(1), (StateMachineState)4)
                ));

            info.SerializeStateMachineStates(cmw);

            var bytes = cmw.ToImmutableArray();
            AssertEx.Equal(new byte[] { 0x06, 0x00, 0x02, 0x10, 0x04, 0x10, 0x00, 0x10, 0x06, 0x20, 0x08, 0x20, 0x0A, 0x30 }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(
                compressedSlotMap: ImmutableArray<byte>.Empty,
                compressedLambdaMap: ImmutableArray<byte>.Empty,
                compressedStateMachineStateMap: bytes).StateMachineStates;

            AssertEx.Equal(new[]
            {
                new StateMachineStateDebugInfo(syntaxOffset: 0x10, new AwaitDebugId(0), (StateMachineState)1),
                new StateMachineStateDebugInfo(syntaxOffset: 0x10, new AwaitDebugId(1), (StateMachineState)2),
                new StateMachineStateDebugInfo(syntaxOffset: 0x10, new AwaitDebugId(2), (StateMachineState)0),
                new StateMachineStateDebugInfo(syntaxOffset: 0x20, new AwaitDebugId(0), (StateMachineState)3),
                new StateMachineStateDebugInfo(syntaxOffset: 0x20, new AwaitDebugId(1), (StateMachineState)4),
                new StateMachineStateDebugInfo(syntaxOffset: 0x30, new AwaitDebugId(0), (StateMachineState)5),
            }, deserialized);
        }

        [Fact]
        public void StateMachineStateDebugInfo_BadData()
        {
            // not sorted:
            Assert.Throws<InvalidDataException>(() => EditAndContinueMethodDebugInformation.Create(
                compressedSlotMap: ImmutableArray<byte>.Empty,
                compressedLambdaMap: ImmutableArray<byte>.Empty,
                compressedStateMachineStateMap: ImmutableArray.Create<byte>(0x06, 0x00, 0x02, 0x20, 0x04, 0x10, 0x00, 0x10, 0x06, 0x20, 0x08, 0x20, 0x0A, 0x30)));
        }

        [Fact]
        public void EncCdiAlignment()
        {
            var slots = ImmutableArray.Create(
               new LocalSlotDebugInfo(SynthesizedLocalKind.UserDefined, new LocalDebugId(-1, 10)),
               new LocalSlotDebugInfo(SynthesizedLocalKind.TryAwaitPendingCaughtException, new LocalDebugId(-20000, 10)));

            var closures = ImmutableArray.Create(
               new ClosureDebugInfo(-100, new DebugId(0, 0)),
               new ClosureDebugInfo(10, new DebugId(1, 0)),
               new ClosureDebugInfo(-200, new DebugId(2, 0)));

            var lambdas = ImmutableArray.Create(
                new LambdaDebugInfo(20, new DebugId(0, 0), 1),
                new LambdaDebugInfo(-50, new DebugId(1, 0), 0),
                new LambdaDebugInfo(-180, new DebugId(2, 0), LambdaDebugInfo.StaticClosureOrdinal));

            var states = ImmutableArray<StateMachineStateDebugInfo>.Empty;

            var debugInfo = new EditAndContinueMethodDebugInformation(1, slots, closures, lambdas, states);

            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);
            Cci.CustomDebugInfoWriter.SerializeCustomDebugInformation(ref cdiEncoder, debugInfo);
            var cdi = cdiEncoder.ToArray();

            Assert.Equal(2, cdiEncoder.RecordCount);

            AssertEx.Equal(new byte[]
            {
                0x04,       // version
                0x02,       // record count
                0x00, 0x00, // alignment

                0x04, // version
                0x06, // record kind
                0x00,
                0x02, // alignment size

                // aligned record size
                0x18, 0x00, 0x00, 0x00,

                // payload (4B aligned)
                0xFF, 0xC0, 0x00, 0x4E,
                0x20, 0x81, 0xC0, 0x00,
                0x4E, 0x1F, 0x0A, 0x9A,
                0x00, 0x0A, 0x00, 0x00,

                0x04, // version
                0x07, // record kind
                0x00,
                0x00, // alignment size

                // aligned record size
                0x18, 0x00, 0x00, 0x00,

                // payload (4B aligned)
                0x02, 0x80, 0xC8, 0x03,
                0x64, 0x80, 0xD2, 0x00,
                0x80, 0xDC, 0x03, 0x80,
                0x96, 0x02, 0x14, 0x01
            }, cdi);

            var deserialized = CustomDebugInfoReader.GetCustomDebugInfoRecords(cdi).ToArray();
            Assert.Equal(CustomDebugInfoKind.EditAndContinueLocalSlotMap, deserialized[0].Kind);
            Assert.Equal(4, deserialized[0].Version);

            Assert.Equal(new byte[]
            {
                0xFF, 0xC0, 0x00, 0x4E,
                0x20, 0x81, 0xC0, 0x00,
                0x4E, 0x1F, 0x0A, 0x9A,
                0x00, 0x0A
            }, deserialized[0].Data);

            Assert.Equal(CustomDebugInfoKind.EditAndContinueLambdaMap, deserialized[1].Kind);
            Assert.Equal(4, deserialized[1].Version);

            Assert.Equal(new byte[]
            {
                0x02, 0x80, 0xC8, 0x03,
                0x64, 0x80, 0xD2, 0x00,
                0x80, 0xDC, 0x03, 0x80,
                0x96, 0x02, 0x14, 0x01
            }, deserialized[1].Data);
        }

        [Fact]
        public void UsingInfo1()
        {
            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);
            cdiEncoder.AddUsingGroups(new int[0]);
            var cdi = cdiEncoder.ToArray();

            Assert.Equal(0, cdiEncoder.RecordCount);
            Assert.Null(cdi);
        }

        [Fact]
        public void UsingInfo2()
        {
            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);
            cdiEncoder.AddUsingGroups(new[] { 1, 2, 3, 4 });
            var cdi = cdiEncoder.ToArray();

            Assert.Equal(1, cdiEncoder.RecordCount);

            AssertEx.Equal(new byte[]
            {
                0x04,       // version
                0x01,       // record count
                0x00, 0x00, // alignment

                0x04, // version
                0x00, // record kind
                0x00,
                0x00,

                // aligned record size
                0x14, 0x00, 0x00, 0x00,
                
                // payload (4B aligned)
                0x04, 0x00, // bucket count
                0x01, 0x00, // using count #1
                0x02, 0x00, // using count #2
                0x03, 0x00, // using count #3
                0x04, 0x00, // using count #4
                0x00, 0x00  // alignment
            }, cdi);
        }

        [Fact]
        public void UsingInfo3()
        {
            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);
            cdiEncoder.AddUsingGroups(new[] { 1, 2, 3 });
            var cdi = cdiEncoder.ToArray();

            Assert.Equal(1, cdiEncoder.RecordCount);

            AssertEx.Equal(new byte[]
            {
                0x04,       // version
                0x01,       // record count
                0x00, 0x00, // alignment

                0x04, // version
                0x00, // record kind
                0x00,
                0x00,

                // aligned record size
                0x10, 0x00, 0x00, 0x00,
                
                // payload (4B aligned)
                0x03, 0x00, // bucket count
                0x01, 0x00, // using count #1
                0x02, 0x00, // using count #2
                0x03, 0x00, // using count #3
            }, cdi);
        }

        [Fact]
        public void ForwardToModuleInfo()
        {
            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);
            cdiEncoder.AddForwardModuleInfo(MetadataTokens.MethodDefinitionHandle(0x123456));
            var cdi = cdiEncoder.ToArray();

            Assert.Equal(1, cdiEncoder.RecordCount);

            AssertEx.Equal(new byte[]
            {
                0x04,       // version
                0x01,       // record count
                0x00, 0x00, // alignment

                0x04, // version
                0x02, // record kind
                0x00,
                0x00,

                // aligned record size
                0x0C, 0x00, 0x00, 0x00,
                
                // payload (4B aligned)
                0x56, 0x34, 0x12, 0x06,
            }, cdi);
        }

        [Fact]
        public void ForwardInfo()
        {
            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);
            cdiEncoder.AddForwardMethodInfo(MetadataTokens.MethodDefinitionHandle(0x123456));
            var cdi = cdiEncoder.ToArray();

            Assert.Equal(1, cdiEncoder.RecordCount);

            AssertEx.Equal(new byte[]
            {
                0x04,       // version
                0x01,       // record count
                0x00, 0x00, // alignment

                0x04, // version
                0x01, // record kind
                0x00,
                0x00,

                // aligned record size
                0x0C, 0x00, 0x00, 0x00,
                
                // payload (4B aligned)
                0x56, 0x34, 0x12, 0x06,
            }, cdi);
        }

        private static byte[] Pad(int length, byte[] array)
        {
            var result = new byte[length];
            Array.Copy(array, 0, result, 0, array.Length);
            return result;
        }

        [Fact]
        public void DynamicLocals()
        {
            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);

            cdiEncoder.AddDynamicLocals(new[]
            {
                ("a", Pad(64, new byte[] { 0x01, 0x02 }), 10, 1),
                ("b", Pad(64, new byte[] { 0xFF }), 1, 2),
            });

            var cdi = cdiEncoder.ToArray();

            Assert.Equal(1, cdiEncoder.RecordCount);

            AssertEx.Equal(new byte[]
            {
                0x04,       // version
                0x01,       // record count
                0x00, 0x00, // alignment

                0x04, // version
                0x05, // record kind
                0x00,
                0x00,

                // aligned record size
                0x9C, 0x01, 0x00, 0x00, 
                
                // payload (4B aligned)

                // locals count
                0x02, 0x00, 0x00, 0x00, 

                // #1

                // flags (64B):
                0x01, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // length
                0x0A, 0x00, 0x00, 0x00,

                // slot index
                0x01, 0x00, 0x00, 0x00,

                // name (64 UTF16 characters)
                0x61, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // #2

                // flags
                0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

                // length
                0x01, 0x00, 0x00, 0x00,

                // slot index
                0x02, 0x00, 0x00, 0x00,

                // name (64 UTF16 characters)
                0x62, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            }, cdi);
        }

        [Fact]
        public void TupleElementNames()
        {
            var builder = new BlobBuilder();
            var cdiEncoder = new CustomDebugInfoEncoder(builder);

            cdiEncoder.AddTupleElementNames(new[]
            {
                (LocalName: "a", SlotIndex: 1, ScopeStart: 0, ScopeEnd: 0, Names: ImmutableArray.Create("e")),
                (LocalName: "b", SlotIndex: -1, ScopeStart: 0, ScopeEnd: 10, Names: ImmutableArray.Create("u", null, "v")),
            });

            var cdi = cdiEncoder.ToArray();

            Assert.Equal(1, cdiEncoder.RecordCount);

            AssertEx.Equal(new byte[]
            {
                0x04,       // version
                0x01,       // record count
                0x00, 0x00, // alignment

                0x04, // version
                0x08, // record kind
                0x00,
                0x01, // alignment size

                // aligned record size
                0x38, 0x00, 0x00, 0x00,

                // payload (4B aligned)
                0x02, 0x00, 0x00, 0x00,   // number of entries

                // entry #1

                0x01, 0x00, 0x00, 0x00,   // element name count
                (byte)'e', 0x00,          // element name 1
                0x01, 0x00, 0x00, 0x00,   // slot index
                0x00, 0x00, 0x00, 0x00,   // scope start 
                0x00, 0x00, 0x00, 0x00,   // scope end
                (byte)'a', 0x00,          // local name

                // entry #2

                0x03, 0x00, 0x00, 0x00,   // element name count  
                (byte)'u', 0x00,          // element name 1
                0x00,                     // element name 2
                (byte)'v', 0x00,          // element name 3

                0xFF, 0xFF, 0xFF, 0xFF,   // slot index
                0x00, 0x00, 0x00, 0x00,   // scope start 
                0x0A, 0x00, 0x00, 0x00,   // scope end   
                (byte)'b', 0x00, 0x00     // local name
            }, cdi);
        }

        [Fact]
        public void InvalidAlignment1()
        {
            // CDIs that don't support alignment:
            var bytes = new byte[]
            {
                0x04, // version
                0x01, // count
                0x00,
                0x00,

                0x04, // version
                0x06, // kind
                0x00,
                0x03, // bad alignment

                // body size
                0x0a, 0x00, 0x00, 0x00,

                // payload
                0x01, 0x00
            };

            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.GetCustomDebugInfoRecords(bytes).ToArray());
        }

        [Fact]
        public void InvalidAlignment2()
        {
            // CDIs that don't support alignment:
            var bytes = new byte[]
            {
                0x04, // version
                0x01, // count
                0x00,
                0x00,

                0x04, // version
                0x06, // kind
                0x00,
                0x03, // bad alignment

                // body size
                0x02, 0x00, 0x00, 0x00,

                // payload
                0x01, 0x00, 0x00, 0x06
            };

            Assert.Throws<InvalidOperationException>(() => CustomDebugInfoReader.GetCustomDebugInfoRecords(bytes).ToArray());
        }

        [Fact]
        public void InvalidAlignment_KindDoesntSupportAlignment()
        {
            // CDIs that don't support alignment:
            var bytes = new byte[]
            {
                0x04, // version
                0x01, // count
                0x00,
                0x00,

                0x04, // version
                0x01, // kind
                0x11, // invalid data
                0x14, // invalid data

                // body size
                0x0c, 0x00, 0x00, 0x00,

                // payload
                0x01, 0x00, 0x00, 0x06
            };

            var records = CustomDebugInfoReader.GetCustomDebugInfoRecords(bytes).ToArray();
            Assert.Equal(1, records.Length);

            Assert.Equal(CustomDebugInfoKind.ForwardMethodInfo, records[0].Kind);
            Assert.Equal(4, records[0].Version);
            AssertEx.Equal(new byte[] { 0x01, 0x00, 0x00, 0x06 }, records[0].Data);
        }
    }
}
