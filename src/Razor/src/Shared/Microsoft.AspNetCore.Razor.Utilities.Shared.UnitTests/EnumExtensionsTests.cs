// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class EnumExtensionsTests
{
    private enum ByteEnum : byte
    {
        Flag1 = 1 << 0,
        Flag2 = 1 << 1,
        Flag3 = 1 << 2,
        Flag4 = 1 << 3,
        Flag5 = 1 << 4,
        Flag6 = 1 << 5,
        Flag7 = 1 << 6,
        Flag8 = 1 << 7
    }

    [Fact]
    public void TestByteSizedEnum()
    {
        ByteEnum actual = 0;
        ByteEnum expected = 0;

        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag1);
        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag2);
        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag3);
        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag4);
        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag5);
        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag6);
        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag7);
        SetFlagAndAssert(ref actual, ref expected, ByteEnum.Flag8);

        Assert.Equal(byte.MaxValue, (byte)actual);
        Assert.Equal(byte.MaxValue, (byte)expected);

        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag1);
        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag2);
        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag3);
        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag4);
        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag5);
        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag6);
        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag7);
        ClearFlagAndAssert(ref actual, ref expected, ByteEnum.Flag8);

        Assert.Equal(0, (byte)actual);
        Assert.Equal(0, (byte)expected);

        static void SetFlagAndAssert(ref ByteEnum actual, ref ByteEnum expected, ByteEnum flag)
        {
            actual.SetFlag(flag);
            expected |= flag;
            Assert.Equal(expected, actual);

            Assert.True(actual.IsFlagSet(flag));
            Assert.False(actual.IsFlagClear(flag));
        }

        static void ClearFlagAndAssert(ref ByteEnum actual, ref ByteEnum expected, ByteEnum flag)
        {
            actual.ClearFlag(flag);
            expected &= ~flag;
            Assert.Equal(expected, actual);

            Assert.False(actual.IsFlagSet(flag));
            Assert.True(actual.IsFlagClear(flag));
        }
    }

    private enum UInt16Enum : ushort
    {
        Flag1 = 1 << 0,
        Flag2 = 1 << 1,
        Flag3 = 1 << 2,
        Flag4 = 1 << 3,
        Flag5 = 1 << 4,
        Flag6 = 1 << 5,
        Flag7 = 1 << 6,
        Flag8 = 1 << 7,
        Flag9 = 1 << 8,
        Flag10 = 1 << 9,
        Flag11 = 1 << 10,
        Flag12 = 1 << 11,
        Flag13 = 1 << 12,
        Flag14 = 1 << 13,
        Flag15 = 1 << 14,
        Flag16 = 1 << 15
    }

    [Fact]
    public void TestUInt16SizedEnum()
    {
        UInt16Enum actual = 0;
        UInt16Enum expected = 0;

        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag1);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag2);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag3);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag4);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag5);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag6);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag7);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag8);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag9);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag10);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag11);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag12);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag13);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag14);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag15);
        SetFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag16);

        Assert.Equal(ushort.MaxValue, (ushort)actual);
        Assert.Equal(ushort.MaxValue, (ushort)expected);

        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag1);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag2);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag3);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag4);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag5);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag6);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag7);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag8);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag9);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag10);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag11);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag12);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag13);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag14);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag15);
        ClearFlagAndAssert(ref actual, ref expected, UInt16Enum.Flag16);

        Assert.Equal(0, (ushort)actual);
        Assert.Equal(0, (ushort)expected);

        static void SetFlagAndAssert(ref UInt16Enum actual, ref UInt16Enum expected, UInt16Enum flag)
        {
            actual.SetFlag(flag);
            expected |= flag;
            Assert.Equal(expected, actual);

            Assert.True(actual.IsFlagSet(flag));
            Assert.False(actual.IsFlagClear(flag));
        }

        static void ClearFlagAndAssert(ref UInt16Enum actual, ref UInt16Enum expected, UInt16Enum flag)
        {
            actual.ClearFlag(flag);
            expected &= ~flag;
            Assert.Equal(expected, actual);

            Assert.False(actual.IsFlagSet(flag));
            Assert.True(actual.IsFlagClear(flag));
        }
    }

    private enum UInt32Enum : uint
    {
        Flag1 = 1u << 0,
        Flag2 = 1u << 1,
        Flag3 = 1u << 2,
        Flag4 = 1u << 3,
        Flag5 = 1u << 4,
        Flag6 = 1u << 5,
        Flag7 = 1u << 6,
        Flag8 = 1u << 7,
        Flag9 = 1u << 8,
        Flag10 = 1u << 9,
        Flag11 = 1u << 10,
        Flag12 = 1u << 11,
        Flag13 = 1u << 12,
        Flag14 = 1u << 13,
        Flag15 = 1u << 14,
        Flag16 = 1u << 15,
        Flag17 = 1u << 16,
        Flag18 = 1u << 17,
        Flag19 = 1u << 18,
        Flag20 = 1u << 19,
        Flag21 = 1u << 20,
        Flag22 = 1u << 21,
        Flag23 = 1u << 22,
        Flag24 = 1u << 23,
        Flag25 = 1u << 24,
        Flag26 = 1u << 25,
        Flag27 = 1u << 26,
        Flag28 = 1u << 27,
        Flag29 = 1u << 28,
        Flag30 = 1u << 29,
        Flag31 = 1u << 30,
        Flag32 = 1u << 31
    }

    [Fact]
    public void TestUInt32SizedEnum()
    {
        UInt32Enum actual = 0;
        UInt32Enum expected = 0;

        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag1);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag2);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag3);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag4);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag5);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag6);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag7);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag8);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag9);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag10);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag11);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag12);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag13);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag14);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag15);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag16);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag17);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag18);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag19);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag20);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag21);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag22);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag23);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag24);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag25);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag26);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag27);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag28);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag29);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag30);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag31);
        SetFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag32);

        Assert.Equal(uint.MaxValue, (uint)actual);
        Assert.Equal(uint.MaxValue, (uint)expected);

        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag1);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag2);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag3);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag4);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag5);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag6);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag7);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag8);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag9);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag10);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag11);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag12);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag13);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag14);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag15);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag16);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag17);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag18);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag19);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag20);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag21);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag22);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag23);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag24);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag25);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag26);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag27);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag28);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag29);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag30);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag31);
        ClearFlagAndAssert(ref actual, ref expected, UInt32Enum.Flag32);

        Assert.Equal(0u, (uint)actual);
        Assert.Equal(0u, (uint)expected);

        static void SetFlagAndAssert(ref UInt32Enum actual, ref UInt32Enum expected, UInt32Enum flag)
        {
            actual.SetFlag(flag);
            expected |= flag;
            Assert.Equal(expected, actual);

            Assert.True(actual.IsFlagSet(flag));
            Assert.False(actual.IsFlagClear(flag));
        }

        static void ClearFlagAndAssert(ref UInt32Enum actual, ref UInt32Enum expected, UInt32Enum flag)
        {
            actual.ClearFlag(flag);
            expected &= ~flag;
            Assert.Equal(expected, actual);

            Assert.False(actual.IsFlagSet(flag));
            Assert.True(actual.IsFlagClear(flag));
        }
    }

    private enum UInt64Enum : ulong
    {
        Flag1 = 1ul << 0,
        Flag2 = 1ul << 1,
        Flag3 = 1ul << 2,
        Flag4 = 1ul << 3,
        Flag5 = 1ul << 4,
        Flag6 = 1ul << 5,
        Flag7 = 1ul << 6,
        Flag8 = 1ul << 7,
        Flag9 = 1ul << 8,
        Flag10 = 1ul << 9,
        Flag11 = 1ul << 10,
        Flag12 = 1ul << 11,
        Flag13 = 1ul << 12,
        Flag14 = 1ul << 13,
        Flag15 = 1ul << 14,
        Flag16 = 1ul << 15,
        Flag17 = 1ul << 16,
        Flag18 = 1ul << 17,
        Flag19 = 1ul << 18,
        Flag20 = 1ul << 19,
        Flag21 = 1ul << 20,
        Flag22 = 1ul << 21,
        Flag23 = 1ul << 22,
        Flag24 = 1ul << 23,
        Flag25 = 1ul << 24,
        Flag26 = 1ul << 25,
        Flag27 = 1ul << 26,
        Flag28 = 1ul << 27,
        Flag29 = 1ul << 28,
        Flag30 = 1ul << 29,
        Flag31 = 1ul << 30,
        Flag32 = 1ul << 31,
        Flag33 = 1ul << 32,
        Flag34 = 1ul << 33,
        Flag35 = 1ul << 34,
        Flag36 = 1ul << 35,
        Flag37 = 1ul << 36,
        Flag38 = 1ul << 37,
        Flag39 = 1ul << 38,
        Flag40 = 1ul << 39,
        Flag41 = 1ul << 40,
        Flag42 = 1ul << 41,
        Flag43 = 1ul << 42,
        Flag44 = 1ul << 43,
        Flag45 = 1ul << 44,
        Flag46 = 1ul << 45,
        Flag47 = 1ul << 46,
        Flag48 = 1ul << 47,
        Flag49 = 1ul << 48,
        Flag50 = 1ul << 49,
        Flag51 = 1ul << 50,
        Flag52 = 1ul << 51,
        Flag53 = 1ul << 52,
        Flag54 = 1ul << 53,
        Flag55 = 1ul << 54,
        Flag56 = 1ul << 55,
        Flag57 = 1ul << 56,
        Flag58 = 1ul << 57,
        Flag59 = 1ul << 58,
        Flag60 = 1ul << 59,
        Flag61 = 1ul << 60,
        Flag62 = 1ul << 61,
        Flag63 = 1ul << 62,
        Flag64 = 1ul << 63
    }

    [Fact]
    public void TestUInt64SizedEnum()
    {
        UInt64Enum actual = 0;
        UInt64Enum expected = 0;

        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag1);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag2);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag3);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag4);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag5);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag6);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag7);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag8);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag9);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag10);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag11);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag12);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag13);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag14);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag15);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag16);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag17);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag18);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag19);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag20);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag21);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag22);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag23);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag24);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag25);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag26);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag27);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag28);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag29);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag30);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag31);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag32);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag33);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag34);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag35);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag36);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag37);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag38);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag39);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag40);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag41);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag42);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag43);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag44);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag45);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag46);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag47);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag48);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag49);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag50);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag51);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag52);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag53);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag54);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag55);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag56);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag57);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag58);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag59);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag60);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag61);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag62);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag63);
        SetFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag64);

        Assert.Equal(ulong.MaxValue, (ulong)actual);
        Assert.Equal(ulong.MaxValue, (ulong)expected);

        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag1);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag2);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag3);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag4);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag5);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag6);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag7);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag8);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag9);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag10);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag11);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag12);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag13);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag14);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag15);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag16);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag17);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag18);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag19);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag20);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag21);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag22);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag23);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag24);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag25);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag26);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag27);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag28);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag29);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag30);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag31);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag32);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag33);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag34);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag35);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag36);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag37);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag38);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag39);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag40);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag41);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag42);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag43);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag44);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag45);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag46);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag47);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag48);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag49);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag50);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag51);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag52);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag53);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag54);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag55);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag56);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag57);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag58);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag59);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag60);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag61);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag62);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag63);
        ClearFlagAndAssert(ref actual, ref expected, UInt64Enum.Flag64);

        Assert.Equal(0ul, (ulong)actual);
        Assert.Equal(0ul, (ulong)expected);

        static void SetFlagAndAssert(ref UInt64Enum actual, ref UInt64Enum expected, UInt64Enum flag)
        {
            actual.SetFlag(flag);
            expected |= flag;
            Assert.Equal(expected, actual);

            Assert.True(actual.IsFlagSet(flag));
            Assert.False(actual.IsFlagClear(flag));
        }

        static void ClearFlagAndAssert(ref UInt64Enum actual, ref UInt64Enum expected, UInt64Enum flag)
        {
            actual.ClearFlag(flag);
            expected &= ~flag;
            Assert.Equal(expected, actual);

            Assert.False(actual.IsFlagSet(flag));
            Assert.True(actual.IsFlagClear(flag));
        }
    }
}
