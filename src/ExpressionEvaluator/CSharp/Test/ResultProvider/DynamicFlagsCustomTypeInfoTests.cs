// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DynamicFlagsCustomTypeInfoTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void ToBytes()
        {
            ValidateToBytes(new bool[0]);

            ValidateToBytes([false]);
            ValidateToBytes([true], 0x01);
            ValidateToBytes([false, false]);
            ValidateToBytes([true, false], 0x01);
            ValidateToBytes([false, true], 0x02);
            ValidateToBytes([true, true], 0x03);

            ValidateToBytes([false, false, true], 0x04);
            ValidateToBytes([false, false, false, true], 0x08);
            ValidateToBytes([false, false, false, false, true], 0x10);
            ValidateToBytes([false, false, false, false, false, true], 0x20);
            ValidateToBytes([false, false, false, false, false, false, true], 0x40);
            ValidateToBytes([false, false, false, false, false, false, false, true], 0x80);
            ValidateToBytes([false, false, false, false, false, false, false, false, true], 0x00, 0x01);
        }

        [Fact]
        public void CopyTo()
        {
            ValidateCopyTo(new byte[0]);

            ValidateCopyTo([0x00], false, false, false, false, false, false, false, false);
            ValidateCopyTo([0x01], true, false, false, false, false, false, false, false);
            ValidateCopyTo([0x02], false, true, false, false, false, false, false, false);
            ValidateCopyTo([0x03], true, true, false, false, false, false, false, false);

            ValidateCopyTo([0x04], false, false, true, false, false, false, false, false);
            ValidateCopyTo([0x08], false, false, false, true, false, false, false, false);
            ValidateCopyTo([0x10], false, false, false, false, true, false, false, false);
            ValidateCopyTo([0x20], false, false, false, false, false, true, false, false);
            ValidateCopyTo([0x40], false, false, false, false, false, false, true, false);
            ValidateCopyTo([0x80], false, false, false, false, false, false, false, true);
            ValidateCopyTo([0x00, 0x01], false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false);
        }

        [Fact]
        public void EncodeAndDecode()
        {
            var encoded = CustomTypeInfo.Encode(null, null);
            Assert.Null(encoded);

            ReadOnlyCollection<byte> bytes;
            ReadOnlyCollection<string> names;

            // Exceed max bytes.
            bytes = GetBytesInRange(0, 256);
            encoded = CustomTypeInfo.Encode(bytes, null);
            Assert.Null(encoded);

            // Max bytes.
            bytes = GetBytesInRange(0, 255);
            encoded = CustomTypeInfo.Encode(bytes, null);
            Assert.Equal(256, encoded.Count);
            Assert.Equal(255, encoded[0]);
            ReadOnlyCollection<byte> dynamicFlags;
            ReadOnlyCollection<string> tupleElementNames;
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Equal(bytes, dynamicFlags);
            Assert.Null(tupleElementNames);

            // Empty dynamic flags collection
            bytes = new ReadOnlyCollection<byte>(new byte[0]);
            // ... with names.
            names = new ReadOnlyCollection<string>(new[] { "A" });
            encoded = CustomTypeInfo.Encode(bytes, names);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Null(dynamicFlags);
            Assert.Equal(names, tupleElementNames);
            // ... without names.
            encoded = CustomTypeInfo.Encode(bytes, null);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Null(dynamicFlags);
            Assert.Null(tupleElementNames);

            // Empty names collection
            names = new ReadOnlyCollection<string>(new string[0]);
            // ... with dynamic flags.
            bytes = GetBytesInRange(0, 255);
            encoded = CustomTypeInfo.Encode(bytes, names);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Equal(bytes, dynamicFlags);
            Assert.Null(tupleElementNames);
            // ... without dynamic flags.
            encoded = CustomTypeInfo.Encode(null, names);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Null(dynamicFlags);
            Assert.Null(tupleElementNames);

            // Single null name
            names = new ReadOnlyCollection<string>(new string[] { null });
            // ... with dynamic flags.
            bytes = GetBytesInRange(0, 255);
            encoded = CustomTypeInfo.Encode(bytes, names);
            Assert.Equal(255, encoded[0]);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Equal(bytes, dynamicFlags);
            Assert.Equal(names, tupleElementNames);
            // ... without dynamic flags.
            encoded = CustomTypeInfo.Encode(null, names);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Null(dynamicFlags);
            Assert.Equal(names, tupleElementNames);

            // Multiple names
            names = new ReadOnlyCollection<string>(new[] { null, "A", null, "B" });
            // ... with dynamic flags.
            bytes = GetBytesInRange(0, 255);
            encoded = CustomTypeInfo.Encode(bytes, names);
            Assert.Equal(255, encoded[0]);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Equal(bytes, dynamicFlags);
            Assert.Equal(names, tupleElementNames);
            // ... without dynamic flags.
            encoded = CustomTypeInfo.Encode(null, names);
            CustomTypeInfo.Decode(CustomTypeInfo.PayloadTypeId, encoded, out dynamicFlags, out tupleElementNames);
            Assert.Null(dynamicFlags);
            Assert.Equal(names, tupleElementNames);
        }

        private static ReadOnlyCollection<byte> GetBytesInRange(int start, int length)
        {
            return new ReadOnlyCollection<byte>(Enumerable.Range(start, length).Select(i => (byte)(i % 256)).ToArray());
        }

        [Fact]
        public void CustomTypeInfoConstructor()
        {
            ValidateCustomTypeInfo();

            ValidateCustomTypeInfo(0x00);
            ValidateCustomTypeInfo(0x01);
            ValidateCustomTypeInfo(0x02);
            ValidateCustomTypeInfo(0x03);

            ValidateCustomTypeInfo(0x04);
            ValidateCustomTypeInfo(0x08);
            ValidateCustomTypeInfo(0x10);
            ValidateCustomTypeInfo(0x20);
            ValidateCustomTypeInfo(0x40);
            ValidateCustomTypeInfo(0x80);
            ValidateCustomTypeInfo(0x00, 0x01);
        }

        [Fact]
        public void CustomTypeInfoConstructor_OtherGuid()
        {
            var customTypeInfo = DkmClrCustomTypeInfo.Create(Guid.NewGuid(), new ReadOnlyCollection<byte>(new byte[] { 0x01 }));
            ReadOnlyCollection<byte> dynamicFlags;
            ReadOnlyCollection<string> tupleElementNames;
            CustomTypeInfo.Decode(
                customTypeInfo.PayloadTypeId,
                customTypeInfo.Payload,
                out dynamicFlags,
                out tupleElementNames);
            Assert.Null(dynamicFlags);
            Assert.Null(tupleElementNames);
        }

        [Fact]
        public void Indexer()
        {
            ValidateIndexer(null);
            ValidateIndexer(false);
            ValidateIndexer(true);
            ValidateIndexer(false, false);
            ValidateIndexer(false, true);
            ValidateIndexer(true, false);
            ValidateIndexer(true, true);

            ValidateIndexer(false, false, true);
            ValidateIndexer(false, false, false, true);
            ValidateIndexer(false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, false, false, true);
            ValidateIndexer(false, false, false, false, false, false, false, false, true);
        }

        [Fact]
        public void SkipOne()
        {
            ValidateBytes(DynamicFlagsCustomTypeInfo.SkipOne(null));

            var dynamicFlagsCustomTypeInfo = new ReadOnlyCollection<byte>(new byte[] { 0x80 });

            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x40);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x20);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x10);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x08);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x04);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x02);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x01);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo);

            dynamicFlagsCustomTypeInfo = new ReadOnlyCollection<byte>(new byte[] { 0x00, 0x02 });

            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x00, 0x01);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x80, 0x00);
            dynamicFlagsCustomTypeInfo = DynamicFlagsCustomTypeInfo.SkipOne(dynamicFlagsCustomTypeInfo);
            ValidateBytes(dynamicFlagsCustomTypeInfo, 0x40, 0x00);
        }

        private static void ValidateCustomTypeInfo(params byte[] payload)
        {
            Assert.NotNull(payload);

            var dkmClrCustomTypeInfo = CustomTypeInfo.Create(new ReadOnlyCollection<byte>(payload), null);
            Assert.Equal(CustomTypeInfo.PayloadTypeId, dkmClrCustomTypeInfo.PayloadTypeId);
            Assert.NotNull(dkmClrCustomTypeInfo.Payload);

            ReadOnlyCollection<byte> dynamicFlags;
            ReadOnlyCollection<string> tupleElementNames;
            CustomTypeInfo.Decode(
                dkmClrCustomTypeInfo.PayloadTypeId,
                dkmClrCustomTypeInfo.Payload,
                out dynamicFlags,
                out tupleElementNames);

            ValidateBytes(dynamicFlags, payload);
            Assert.Null(tupleElementNames);
        }

        private static void ValidateIndexer(params bool[] dynamicFlags)
        {
            if (dynamicFlags == null)
            {
                Assert.False(DynamicFlagsCustomTypeInfo.GetFlag(null, 0));
            }
            else
            {
                var builder = ArrayBuilder<bool>.GetInstance(dynamicFlags.Length);
                builder.AddRange(dynamicFlags);
                var customTypeInfo = DynamicFlagsCustomTypeInfo.ToBytes(builder);
                builder.Free();

                AssertEx.All(dynamicFlags.Select((f, i) => f == DynamicFlagsCustomTypeInfo.GetFlag(customTypeInfo, i)), x => x);
                Assert.False(DynamicFlagsCustomTypeInfo.GetFlag(customTypeInfo, dynamicFlags.Length));
            }
        }

        private static void ValidateToBytes(bool[] dynamicFlags, params byte[] expectedBytes)
        {
            Assert.NotNull(dynamicFlags);
            Assert.NotNull(expectedBytes);

            var builder = ArrayBuilder<bool>.GetInstance(dynamicFlags.Length);
            builder.AddRange(dynamicFlags);
            var actualBytes = DynamicFlagsCustomTypeInfo.ToBytes(builder);
            builder.Free();
            ValidateBytes(actualBytes, expectedBytes);
        }

        private static void ValidateCopyTo(byte[] dynamicFlags, params bool[] expectedFlags)
        {
            var builder = ArrayBuilder<bool>.GetInstance();
            DynamicFlagsCustomTypeInfo.CopyTo(new ReadOnlyCollection<byte>(dynamicFlags), builder);
            var actualFlags = builder.ToArrayAndFree();
            Assert.Equal(expectedFlags, actualFlags);
        }

        private static void ValidateBytes(ReadOnlyCollection<byte> actualBytes, params byte[] expectedBytes)
        {
            Assert.NotNull(expectedBytes);

            if (expectedBytes.Length == 0)
            {
                Assert.Null(actualBytes);
            }
            else
            {
                Assert.Equal(expectedBytes, actualBytes);
            }
        }
    }
}
