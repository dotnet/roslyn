// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cci;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.PEWriter
{
    public class BinaryWriterTests
    {
        private static byte[] CompressUnsignedInteger(int value)
        {
            var writer = new BlobWriter();
            writer.WriteCompressedUInt((uint)value);
            return writer.ToArray();
        }

        private static byte[] CompressSignedInteger(int value)
        {
            var writer = new BlobWriter();
            writer.WriteCompressedSignedInteger(value);
            return writer.ToArray();
        }

        [Fact]
        public void CompressUnsignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.

            AssertEx.Equal(new byte[] { 0x00 }, CompressUnsignedInteger(0));
            AssertEx.Equal(new byte[] { 0x03 }, CompressUnsignedInteger(0x03));
            AssertEx.Equal(new byte[] { 0x7f }, CompressUnsignedInteger(0x7F));
            AssertEx.Equal(new byte[] { 0x80, 0x80 }, CompressUnsignedInteger(0x80));
            AssertEx.Equal(new byte[] { 0xAE, 0x57 }, CompressUnsignedInteger(0x2E57));
            AssertEx.Equal(new byte[] { 0xBF, 0xFF }, CompressUnsignedInteger(0x3FFF));
            AssertEx.Equal(new byte[] { 0xC0, 0x00, 0x40, 0x00 }, CompressUnsignedInteger(0x4000));
            AssertEx.Equal(new byte[] { 0xDF, 0xFF, 0xFF, 0xFF }, CompressUnsignedInteger(0x1FFFFFFF));
        }

        [Fact]
        public void CompressSignedIntegersFromSpecExamples()
        {
            // These examples are straight from the CLI spec.
            AssertEx.Equal(new byte[] { 0x00 }, CompressSignedInteger(0));
            AssertEx.Equal(new byte[] { 0x02 }, CompressSignedInteger(1));
            AssertEx.Equal(new byte[] { 0x06 }, CompressSignedInteger(3));
            AssertEx.Equal(new byte[] { 0x7f }, CompressSignedInteger(-1));
            AssertEx.Equal(new byte[] { 0x7b }, CompressSignedInteger(-3));
            AssertEx.Equal(new byte[] { 0x80, 0x80 }, CompressSignedInteger(64));
            AssertEx.Equal(new byte[] { 0x01 }, CompressSignedInteger(-64));
            AssertEx.Equal(new byte[] { 0xC0, 0x00, 0x40, 0x00 }, CompressSignedInteger(8192));
            AssertEx.Equal(new byte[] { 0x80, 0x01 }, CompressSignedInteger(-8192));
            AssertEx.Equal(new byte[] { 0xDF, 0xFF, 0xFF, 0xFE }, CompressSignedInteger(268435455));
            AssertEx.Equal(new byte[] { 0xC0, 0x00, 0x00, 0x01 }, CompressSignedInteger(-268435456));
        }
    }
}
