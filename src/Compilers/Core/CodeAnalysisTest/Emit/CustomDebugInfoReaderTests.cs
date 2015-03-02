﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;


using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using PDB::Microsoft.VisualStudio.SymReaderInterop;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Emit
{
    public class CustomDebugInfoReaderTests
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

            var customMetadata = new Cci.MemoryStream();
            var cmw = new Cci.BinaryWriter(customMetadata);

            new EditAndContinueMethodDebugInformation(123, slots, closures, lambdas).SerializeLocalSlots(cmw);

            var bytes = customMetadata.ToImmutableArray();
            AssertEx.Equal(new byte[] { 0xFF, 0xC0, 0x00, 0x4E, 0x20, 0x81, 0xC0, 0x00, 0x4E, 0x1F, 0x0A, 0x9A, 0x00, 0x0A }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(bytes, default(ImmutableArray<byte>)).LocalSlots;

            AssertEx.Equal(slots, deserialized);
        }

        [Fact]
        public void EditAndContinueLambdaAndClosureMap_NegativeSyntaxOffsets()
        {
            var slots = ImmutableArray<LocalSlotDebugInfo>.Empty;

            var closures = ImmutableArray.Create(
                new ClosureDebugInfo(-100),
                new ClosureDebugInfo(10),
                new ClosureDebugInfo(-200));

            var lambdas = ImmutableArray.Create(
                new LambdaDebugInfo(20, 1),
                new LambdaDebugInfo(-50, 0),
                new LambdaDebugInfo(-180, -1));

            var customMetadata = new Cci.MemoryStream();
            var cmw = new Cci.BinaryWriter(customMetadata);

            new EditAndContinueMethodDebugInformation(0x7b, slots, closures, lambdas).SerializeLambdaMap(cmw);

            var bytes = customMetadata.ToImmutableArray();

            AssertEx.Equal(new byte[] { 0x7C, 0x80, 0xC8, 0x03, 0x64, 0x80, 0xD2, 0x00, 0x80, 0xDC, 0x02, 0x80, 0x96, 0x01, 0x14, 0x00 }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(default(ImmutableArray<byte>), bytes);

            AssertEx.Equal(closures, deserialized.Closures);
            AssertEx.Equal(lambdas, deserialized.Lambdas);
        }

        [Fact]
        public void EditAndContinueLambdaAndClosureMap_NoClosures()
        {
            var slots = ImmutableArray<LocalSlotDebugInfo>.Empty;

            var closures = ImmutableArray<ClosureDebugInfo>.Empty;
            var lambdas = ImmutableArray.Create(new LambdaDebugInfo(20, -1));

            var customMetadata = new Cci.MemoryStream();
            var cmw = new Cci.BinaryWriter(customMetadata);

            new EditAndContinueMethodDebugInformation(-1, slots, closures, lambdas).SerializeLambdaMap(cmw);

            var bytes = customMetadata.ToImmutableArray();

            AssertEx.Equal(new byte[] { 0x00, 0x01, 0x00, 0x15, 0x00 }, bytes);

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

            var customMetadata = new Cci.MemoryStream();
            var cmw = new Cci.BinaryWriter(customMetadata);

            new EditAndContinueMethodDebugInformation(10, slots, closures, lambdas).SerializeLambdaMap(cmw);

            var bytes = customMetadata.ToImmutableArray();

            AssertEx.Equal(new byte[] { 0x0B, 0x01, 0x00 }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(default(ImmutableArray<byte>), bytes);

            AssertEx.Equal(closures, deserialized.Closures);
            AssertEx.Equal(lambdas, deserialized.Lambdas);
        }
    }
}
