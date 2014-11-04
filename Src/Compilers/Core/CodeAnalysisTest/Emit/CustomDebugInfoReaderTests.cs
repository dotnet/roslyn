// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PDB;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using PDB::Roslyn.Utilities.Pdb;
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

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(new byte[0], CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);
            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(new byte[] { 1 }, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);
            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(new byte[] { 1, 2 }, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

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

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

            // invalid record size = Int32.MinValue
            cdi = new byte[]
            {
                4, 1, 0, 0, // global header
                /*version*/4, /*kind*/(byte)CustomDebugInfoKind.EditAndContinueLocalSlotMap, /*padding*/0, 0, /*size:*/ 0x00, 0x00, 0x00, 0x80,
                0, 0, 0, 0
            };

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

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

            Assert.True(CustomDebugInfoReader.TryGetCustomDebugInfoRecord(cdi, CustomDebugInfoKind.EditAndContinueLocalSlotMap).IsDefault);

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
        public void EditAndContinueLocalSlotMap_NegativeSyntaxOffsets()
        {
            var slots = ImmutableArray.Create(
                new LocalSlotDebugInfo(SynthesizedLocalKind.UserDefined, new LocalDebugId(-1, 10)),
                new LocalSlotDebugInfo(SynthesizedLocalKind.TryAwaitPendingCaughtException, new LocalDebugId(-20000, 10)));

            var customMetadata = new Cci.MemoryStream();
            var cmw = new Cci.BinaryWriter(customMetadata);

            new EditAndContinueMethodDebugInformation(slots).SerializeLocalSlots(cmw);

            var bytes = customMetadata.ToImmutableArray();
            AssertEx.Equal(new byte[] { 0xFE, 0xC0, 0x00, 0x4E, 0x20, 0x81, 0xC0, 0x00, 0x4E, 0x1F, 0x0A, 0x9A, 0x00, 0x0A }, bytes);

            var deserialized = EditAndContinueMethodDebugInformation.Create(bytes).LocalSlots;

            AssertEx.Equal(slots, deserialized);
        }
    }
}
