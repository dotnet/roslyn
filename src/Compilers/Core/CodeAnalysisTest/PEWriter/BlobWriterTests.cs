// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.Cci;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.PEWriter
{
    public class BlobWriterTests
    {
        private static byte[] CompressUnsignedInteger(int value)
        {
            var writer = new BlobWriter();
            writer.WriteCompressedInteger((uint)value);
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

        [Fact]
        public void WritePrimitive()
        {
            var writer = new BlobWriter(4);

            writer.WriteUInt32(0x11223344);
            writer.WriteUInt16(0x5566);
            writer.WriteByte(0x77);
            writer.WriteUInt64(0x8899aabbccddeeff);
            writer.WriteInt32(-1);
            writer.WriteInt16(-2);
            writer.WriteSByte(-3);
            writer.WriteBoolean(true);
            writer.WriteBoolean(false);
            writer.WriteInt64(unchecked((long)0xfedcba0987654321));
            writer.WriteDateTime(new DateTime(0x1112223334445556));
            writer.WriteDecimal(102030405060.70m);
            writer.WriteDouble(double.NaN);
            writer.WriteSingle(float.NegativeInfinity);

            AssertEx.Equal(new byte[] 
            {
                0x44, 0x33, 0x22, 0x11,
                0x66, 0x55,
                0x77,
                0xff, 0xee, 0xdd, 0xcc, 0xbb, 0xaa, 0x99, 0x88,
                0xff, 0xff, 0xff, 0xff, 
                0xfe, 0xff,
                0xfd,
                0x01,
                0x00,
                0x21, 0x43, 0x65, 0x87, 0x09, 0xBA, 0xDC, 0xFE,
                0x56, 0x55, 0x44, 0x34, 0x33, 0x22, 0x12, 0x11,
                0x02, 0xD6, 0xE0, 0x9A, 0x94, 0x47, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF8, 0xFF,
                0x00, 0x00, 0x80, 0xFF
            }, writer.ToArray());
        }

        [Fact]
        public void WriteBytes1()
        {
            var writer = new BlobWriter(4);

            writer.WriteBytes(new byte[] { 1, 2, 3, 4 });
            writer.WriteBytes(new byte[] { });
            writer.WriteBytes(new byte[] { }, 0, 0);
            writer.WriteBytes(new byte[] { 5, 6, 7, 8 });
            writer.WriteBytes(new byte[] { 9 });
            writer.WriteBytes(new byte[] { 0x0a }, 0, 0);
            writer.WriteBytes(new byte[] { 0x0b }, 0, 1);
            writer.WriteBytes(new byte[] { 0x0c }, 1, 0);
            writer.WriteBytes(new byte[] { 0x0d, 0x0e }, 1, 1);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x02, 0x03, 0x04,
                0x05, 0x06, 0x07, 0x08,
                0x09,
                0x0b,
                0x0e
            }, writer.ToArray());
        }

        [Fact]
        public void WriteBytes2()
        {
            var writer = new BlobWriter(4);

            writer.WriteBytes(0xff, 0);
            writer.WriteBytes(1, 4);
            writer.WriteBytes(0xff, 0);
            writer.WriteBytes(2, 10);
            writer.WriteBytes(0xff, 0);
            writer.WriteBytes(3, 1);
            
            AssertEx.Equal(new byte[]
            {
                0x01, 0x01, 0x01, 0x01,
                0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
                0x03
            }, writer.ToArray());
        }

        [Fact]
        public void WriteAlignPad()
        {
            var writer = new BlobWriter(4);

            writer.WriteByte(0x01);
            writer.PadTo(2);
            writer.WriteByte(0x02);
            writer.Align(4);
            writer.Align(4);

            writer.WriteByte(0x03);
            writer.Align(4);

            writer.WriteByte(0x04);
            writer.WriteByte(0x05);
            writer.Align(8);

            writer.WriteByte(0x06);
            writer.Align(2);
            writer.Align(1);

            AssertEx.Equal(new byte[]
            {
                0x01, 0x00, 0x02, 0x00,
                0x03, 0x00, 0x00, 0x00,
                0x04, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x06, 0x00
            }, writer.ToArray());
        }

        [Fact]
        public void WriteUTF8()
        {
            var writer = new BlobWriter(4);
            writer.WriteUTF8("a");
            writer.WriteUTF8("");
            writer.WriteUTF8("bc");
            writer.WriteUTF8("d");
            writer.WriteUTF8("");

            writer.WriteUTF8(Encoding.UTF8.GetString(new byte[] 
            {
                0x00,
                0xC2, 0x80,
                0xE1, 0x88, 0xB4
            }));

            writer.WriteUTF8("\0\ud800"); // hi surrogate
            writer.WriteUTF8("\0\udc00"); // lo surrogate
            writer.WriteUTF8("\0\ud800\udc00"); // pair
            writer.WriteUTF8("\0\udc00\ud800"); // lo + hi

            AssertEx.Equal(new byte[]
            {
                (byte)'a',
                (byte)'b', (byte)'c',
                (byte)'d',
                0x00, 0xC2, 0x80, 0xE1, 0x88, 0xB4,

                0x00, 0xED, 0xA0, 0x80,
                0x00, 0xED, 0xB0, 0x80,
                0x00, 0xF0, 0x90, 0x80, 0x80,
                0x00, 0xED, 0xB0, 0x80, 0xED, 0xA0, 0x80

            }, writer.ToArray());
        }
    }
}
