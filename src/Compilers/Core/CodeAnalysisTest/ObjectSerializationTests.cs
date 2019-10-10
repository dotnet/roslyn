// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class ObjectSerializationTests
    {
        static ObjectSerializationTests()
        {
            // Register appropriate deserialization methods.
            new PrimitiveArrayMemberTest();
            new PrimitiveMemberTest();
            new PrimitiveValueTest();
        }

        [Fact]
        private void TestInvalidStreamVersion()
        {
            var stream = new MemoryStream();
            stream.WriteByte(0);
            stream.WriteByte(0);

            stream.Position = 0;

            var reader = ObjectReader.TryGetReader(stream);
            Assert.Null(reader);
        }

        private void RoundTrip(Action<ObjectWriter> writeAction, Action<ObjectReader> readAction, bool recursive)
        {
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);

            writeAction(writer);
            writer.Dispose();

            stream.Position = 0;
            using (var reader = ObjectReader.TryGetReader(stream))
            {
                readAction(reader);
            }
        }

        private void TestRoundTrip(Action<ObjectWriter> writeAction, Action<ObjectReader> readAction)
        {
            RoundTrip(writeAction, readAction, recursive: true);
            RoundTrip(writeAction, readAction, recursive: false);
        }

        private T RoundTrip<T>(T value, Action<ObjectWriter, T> writeAction, Func<ObjectReader, T> readAction, bool recursive)
        {
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);

            writeAction(writer, value);
            writer.Dispose();

            stream.Position = 0;
            using (var reader = ObjectReader.TryGetReader(stream))
            {
                return (T)readAction(reader);
            }
        }

        private void TestRoundTrip<T>(T value, Action<ObjectWriter, T> writeAction, Func<ObjectReader, T> readAction, bool recursive)
        {
            var newValue = RoundTrip(value, writeAction, readAction, recursive);
            Assert.True(Equalish(value, newValue));
        }

        private void TestRoundTrip<T>(T value, Action<ObjectWriter, T> writeAction, Func<ObjectReader, T> readAction)
        {
            TestRoundTrip(value, writeAction, readAction, recursive: true);
            TestRoundTrip(value, writeAction, readAction, recursive: false);
        }

        private T RoundTripValue<T>(T value, bool recursive)
        {
            return RoundTrip(value,
                (w, v) =>
                {
                    if (v != null && v.GetType().IsEnum)
                    {
                        w.WriteInt64(Convert.ToInt64((object)v));
                    }
                    else
                    {
                        w.WriteValue(v);
                    }
                },
                r => value != null && value.GetType().IsEnum
                    ? (T)Enum.ToObject(typeof(T), r.ReadInt64())
                    : (T)r.ReadValue(), recursive);
        }

        private void TestRoundTripValue<T>(T value, bool recursive)
        {
            var newValue = RoundTripValue(value, recursive);
            Assert.True(Equalish(value, newValue));
        }

        private void TestRoundTripValue<T>(T value)
        {
            TestRoundTripValue(value, recursive: true);
            TestRoundTripValue(value, recursive: false);
        }

        private static bool Equalish<T>(T value1, T value2)
        {
            return object.Equals(value1, value2)
                || (value1 is Array && value2 is Array && ArrayEquals((Array)(object)value1, (Array)(object)value2));
        }

        private static bool ArrayEquals(Array seq1, Array seq2)
        {
            if (seq1 == null && seq2 == null)
            {
                return true;
            }
            else if (seq1 == null || seq2 == null)
            {
                return false;
            }

            if (seq1.Length != seq2.Length)
            {
                return false;
            }

            for (int i = 0; i < seq1.Length; i++)
            {
                if (!Equalish(seq1.GetValue(i), seq2.GetValue(i)))
                {
                    return false;
                }
            }

            return true;
        }

        private class TypeWithOneMember<T> : IObjectWritable, IEquatable<TypeWithOneMember<T>>
        {
            private T _member;

            public TypeWithOneMember(T value)
            {
                _member = value;
            }

            private TypeWithOneMember(ObjectReader reader)
            {
                _member = typeof(T).IsEnum
                    ? (T)Enum.ToObject(typeof(T), reader.ReadInt64())
                    : (T)reader.ReadValue();
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                if (typeof(T).IsEnum)
                {
                    writer.WriteInt64(Convert.ToInt64(_member));
                }
                else
                {
                    writer.WriteValue(_member);
                }
            }

            static TypeWithOneMember()
            {
                ObjectBinder.RegisterTypeReader(typeof(TypeWithOneMember<T>), r => new TypeWithOneMember<T>(r));
            }

            public override Int32 GetHashCode()
            {
                if (_member == null)
                {
                    return 0;
                }
                else
                {
                    return _member.GetHashCode();
                }
            }

            public override Boolean Equals(Object obj)
            {
                return Equals(obj as TypeWithOneMember<T>);
            }

            public bool Equals(TypeWithOneMember<T> other)
            {
                return other != null && Equalish(_member, other._member);
            }
        }

        private class TypeWithTwoMembers<T, S> : IObjectWritable, IEquatable<TypeWithTwoMembers<T, S>>
        {
            private T _member1;
            private S _member2;

            public TypeWithTwoMembers(T value1, S value2)
            {
                _member1 = value1;
                _member2 = value2;
            }

            private TypeWithTwoMembers(ObjectReader reader)
            {
                _member1 = (T)reader.ReadValue();
                _member2 = (S)reader.ReadValue();
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                writer.WriteValue(_member1);
                writer.WriteValue(_member2);
            }

            static TypeWithTwoMembers()
            {
                ObjectBinder.RegisterTypeReader(typeof(TypeWithTwoMembers<T, S>), r => new TypeWithTwoMembers<T, S>(r));
            }

            public override int GetHashCode()
            {
                if (_member1 == null)
                {
                    return 0;
                }
                else
                {
                    return _member1.GetHashCode();
                }
            }

            public override Boolean Equals(Object obj)
            {
                return Equals(obj as TypeWithTwoMembers<T, S>);
            }

            public bool Equals(TypeWithTwoMembers<T, S> other)
            {
                return other != null
                    && Equalish(_member1, other._member1)
                    && Equalish(_member2, other._member2);
            }
        }

        // this type simulates a class with many members.. 
        // it serializes each member individually, not as an array.
        private class TypeWithManyMembers<T> : IObjectWritable, IEquatable<TypeWithManyMembers<T>>
        {
            private T[] _members;

            public TypeWithManyMembers(T[] values)
            {
                _members = values;
            }

            private TypeWithManyMembers(ObjectReader reader)
            {
                var count = reader.ReadInt32();
                _members = new T[count];

                for (int i = 0; i < count; i++)
                {
                    _members[i] = (T)reader.ReadValue();
                }
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(_members.Length);

                for (int i = 0; i < _members.Length; i++)
                {
                    writer.WriteValue(_members[i]);
                }
            }

            static TypeWithManyMembers()
            {
                ObjectBinder.RegisterTypeReader(typeof(TypeWithManyMembers<T>), r => new TypeWithManyMembers<T>(r));
            }

            public override int GetHashCode()
            {
                return _members.Length;
            }

            public override Boolean Equals(Object obj)
            {
                return Equals(obj as TypeWithManyMembers<T>);
            }

            public bool Equals(TypeWithManyMembers<T> other)
            {
                if (other == null)
                {
                    return false;
                }

                if (_members.Length != other._members.Length)
                {
                    return false;
                }

                return Equalish(_members, other._members);
            }
        }

        private void TestRoundTripMember<T>(T value)
        {
            TestRoundTripValue(new TypeWithOneMember<T>(value));
        }

        private void TestRoundTripMembers<T, S>(T value1, S value2)
        {
            TestRoundTripValue(new TypeWithTwoMembers<T, S>(value1, value2));
        }

        private void TestRoundTripMembers<T>(params T[] values)
        {
            TestRoundTripValue(new TypeWithManyMembers<T>(values));
        }

        [Fact]
        public void TestValueInt32()
        {
            TestRoundTripValue(123);
        }

        [Fact]
        public void TestMemberInt32()
        {
            TestRoundTripMember(123);
        }

        [Fact]
        public void TestMemberIntString()
        {
            TestRoundTripMembers(123, "Hello");
        }

        [Fact]
        public void TestManyMembersInt32()
        {
            TestRoundTripMembers(Enumerable.Range(0, 1000).ToArray());
        }

        [Fact]
        public void TestSmallArrayMember()
        {
            TestRoundTripMember(Enumerable.Range(0, 3).ToArray());
        }

        [Fact]
        public void TestEmptyArrayMember()
        {
            TestRoundTripMember(new int[] { });
        }

        [Fact]
        public void TestNullArrayMember()
        {
            TestRoundTripMember<int[]>(null);
        }

        [Fact]
        public void TestLargeArrayMember()
        {
            TestRoundTripMember(Enumerable.Range(0, 1000).ToArray());
        }

        [Fact]
        public void TestEnumMember()
        {
            TestRoundTripMember(EByte.Value);
        }

        [Fact]
        public void TestInt32EncodingKinds()
        {
            Assert.Equal(ObjectWriter.EncodingKind.Int32_1, ObjectWriter.EncodingKind.Int32_0 + 1);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_2, ObjectWriter.EncodingKind.Int32_0 + 2);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_3, ObjectWriter.EncodingKind.Int32_0 + 3);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_4, ObjectWriter.EncodingKind.Int32_0 + 4);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_5, ObjectWriter.EncodingKind.Int32_0 + 5);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_6, ObjectWriter.EncodingKind.Int32_0 + 6);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_7, ObjectWriter.EncodingKind.Int32_0 + 7);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_8, ObjectWriter.EncodingKind.Int32_0 + 8);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_9, ObjectWriter.EncodingKind.Int32_0 + 9);
            Assert.Equal(ObjectWriter.EncodingKind.Int32_10, ObjectWriter.EncodingKind.Int32_0 + 10);
        }

        [Fact]
        public void TestUInt32EncodingKinds()
        {
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_1, ObjectWriter.EncodingKind.UInt32_0 + 1);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_2, ObjectWriter.EncodingKind.UInt32_0 + 2);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_3, ObjectWriter.EncodingKind.UInt32_0 + 3);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_4, ObjectWriter.EncodingKind.UInt32_0 + 4);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_5, ObjectWriter.EncodingKind.UInt32_0 + 5);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_6, ObjectWriter.EncodingKind.UInt32_0 + 6);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_7, ObjectWriter.EncodingKind.UInt32_0 + 7);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_8, ObjectWriter.EncodingKind.UInt32_0 + 8);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_9, ObjectWriter.EncodingKind.UInt32_0 + 9);
            Assert.Equal(ObjectWriter.EncodingKind.UInt32_10, ObjectWriter.EncodingKind.UInt32_0 + 10);
        }

        private void TestRoundTripType(Type type)
        {
            TestRoundTrip(type, (w, v) => w.WriteType(v), r => r.ReadType());
        }

        [Fact]
        public void TestTypes()
        {
            TestRoundTripType(typeof(int));
            TestRoundTripType(typeof(string));
            TestRoundTripType(typeof(ObjectSerializationTests));
        }

        private void TestRoundTripCompressedUint(uint value)
        {
            TestRoundTrip(value, (w, v) => ((ObjectWriter)w).WriteCompressedUInt(v), r => ((ObjectReader)r).ReadCompressedUInt());
        }

        [Fact]
        public void TestCompressedUInt()
        {
            TestRoundTripCompressedUint(0);
            TestRoundTripCompressedUint(0x01u);
            TestRoundTripCompressedUint(0x0123u);     // unique bytes tests order
            TestRoundTripCompressedUint(0x012345u);   // unique bytes tests order
            TestRoundTripCompressedUint(0x01234567u); // unique bytes tests order
            TestRoundTripCompressedUint(0x3Fu);       // largest value packed in one byte
            TestRoundTripCompressedUint(0x3FFFu);     // largest value packed into two bytes
            TestRoundTripCompressedUint(0x3FFFFFu);   // no three byte option yet, but test anyway
            TestRoundTripCompressedUint(0x3FFFFFFFu); // largest unit allowed in four bytes

            Assert.Throws<ArgumentException>(() => TestRoundTripCompressedUint(uint.MaxValue)); // max uint not allowed
            Assert.Throws<ArgumentException>(() => TestRoundTripCompressedUint(0x80000000u)); // highest bit set not allowed
            Assert.Throws<ArgumentException>(() => TestRoundTripCompressedUint(0x40000000u)); // second highest bit set not allowed
            Assert.Throws<ArgumentException>(() => TestRoundTripCompressedUint(0xC0000000u)); // both high bits set not allowed
        }

        [Fact]
        public void TestArraySizes()
        {
            TestArrayValues<byte>(1, 2, 3, 4, 5);
            TestArrayValues<sbyte>(1, 2, 3, 4, 5);
            TestArrayValues<short>(1, 2, 3, 4, 5);
            TestArrayValues<ushort>(1, 2, 3, 4, 5);
            TestArrayValues<int>(1, 2, 3, 4, 5);
            TestArrayValues<uint>(1, 2, 3, 4, 5);
            TestArrayValues<long>(1, 2, 3, 4, 5);
            TestArrayValues<ulong>(1, 2, 3, 4, 5);
            TestArrayValues<decimal>(1m, 2m, 3m, 4m, 5m);
            TestArrayValues<float>(1.0f, 2.0f, 3.0f, 4.0f, 5.0f);
            TestArrayValues<double>(1.0, 2.0, 3.0, 4.0, 5.0);
            TestArrayValues<char>('1', '2', '3', '4', '5');
            TestArrayValues<string>("1", "2", "3", "4", "5");
            TestArrayValues(
                new TypeWithOneMember<int>(1),
                new TypeWithOneMember<int>(2),
                new TypeWithOneMember<int>(3),
                new TypeWithOneMember<int>(4),
                new TypeWithOneMember<int>(5));
        }

        private void TestArrayValues<T>(T v1, T v2, T v3, T v4, T v5)
        {
            TestRoundTripValue((T[])null);
            TestRoundTripValue(new T[] { });
            TestRoundTripValue(new T[] { v1 });
            TestRoundTripValue(new T[] { v1, v2 });
            TestRoundTripValue(new T[] { v1, v2, v3 });
            TestRoundTripValue(new T[] { v1, v2, v3, v4 });
            TestRoundTripValue(new T[] { v1, v2, v3, v4, v5 });
        }

        [Fact]
        public void TestPrimitiveArrayValues()
        {
            TestRoundTrip(w => TestWritingPrimitiveArrays(w), r => TestReadingPrimitiveArrays(r));
        }

        [Fact]
        public void TestPrimitiveArrayMembers()
        {
            TestRoundTrip(w => w.WriteValue(new PrimitiveArrayMemberTest()), r => r.ReadValue());
        }

        public class PrimitiveArrayMemberTest : IObjectWritable
        {
            public PrimitiveArrayMemberTest()
            {
            }

            private PrimitiveArrayMemberTest(ObjectReader reader)
            {
                TestReadingPrimitiveArrays(reader);
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                TestWritingPrimitiveArrays(writer);
            }

            static PrimitiveArrayMemberTest()
            {
                ObjectBinder.RegisterTypeReader(typeof(PrimitiveArrayMemberTest), r => new PrimitiveArrayMemberTest(r));
            }
        }

        private static void TestWritingPrimitiveArrays(ObjectWriter writer)
        {
            var inputBool = new bool[] { true, false };
            var inputByte = new byte[] { 1, 2, 3, 4, 5 };
            var inputChar = new char[] { 'h', 'e', 'l', 'l', 'o' };
            var inputDecimal = new decimal[] { 1.0M, 2.0M, 3.0M, 4.0M, 5.0M };
            var inputDouble = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var inputFloat = new float[] { 1.0F, 2.0F, 3.0F, 4.0F, 5.0F };
            var inputInt = new int[] { -1, -2, -3, -4, -5 };
            var inputLong = new long[] { 1, 2, 3, 4, 5 };
            var inputSByte = new sbyte[] { -1, -2, -3, -4, -5 };
            var inputShort = new short[] { -1, -2, -3, -4, -5 };
            var inputUInt = new uint[] { 1, 2, 3, 4, 5 };
            var inputULong = new ulong[] { 1, 2, 3, 4, 5 };
            var inputUShort = new ushort[] { 1, 2, 3, 4, 5 };
            var inputString = new string[] { "h", "e", "l", "l", "o" };

            writer.WriteValue(inputBool);
            writer.WriteValue(inputByte);
            writer.WriteValue(inputChar);
            writer.WriteValue(inputDecimal);
            writer.WriteValue(inputDouble);
            writer.WriteValue(inputFloat);
            writer.WriteValue(inputInt);
            writer.WriteValue(inputLong);
            writer.WriteValue(inputSByte);
            writer.WriteValue(inputShort);
            writer.WriteValue(inputUInt);
            writer.WriteValue(inputULong);
            writer.WriteValue(inputUShort);
            writer.WriteValue(inputString);
        }

        private static void TestReadingPrimitiveArrays(ObjectReader reader)
        {
            var inputBool = new bool[] { true, false };
            var inputByte = new byte[] { 1, 2, 3, 4, 5 };
            var inputChar = new char[] { 'h', 'e', 'l', 'l', 'o' };
            var inputDecimal = new decimal[] { 1.0M, 2.0M, 3.0M, 4.0M, 5.0M };
            var inputDouble = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
            var inputFloat = new float[] { 1.0F, 2.0F, 3.0F, 4.0F, 5.0F };
            var inputInt = new int[] { -1, -2, -3, -4, -5 };
            var inputLong = new long[] { 1, 2, 3, 4, 5 };
            var inputSByte = new sbyte[] { -1, -2, -3, -4, -5 };
            var inputShort = new short[] { -1, -2, -3, -4, -5 };
            var inputUInt = new uint[] { 1, 2, 3, 4, 5 };
            var inputULong = new ulong[] { 1, 2, 3, 4, 5 };
            var inputUShort = new ushort[] { 1, 2, 3, 4, 5 };
            var inputString = new string[] { "h", "e", "l", "l", "o" };

            Assert.True(Enumerable.SequenceEqual(inputBool, (bool[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputByte, (byte[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputChar, (char[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputDecimal, (decimal[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputDouble, (double[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputFloat, (float[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputInt, (int[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputLong, (long[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputSByte, (sbyte[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputShort, (short[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputUInt, (uint[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputULong, (ulong[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputUShort, (ushort[])reader.ReadValue()));
            Assert.True(Enumerable.SequenceEqual(inputString, (string[])reader.ReadValue()));
        }

        [Fact]
        public void TestBooleanArrays()
        {
            for (var i = 0; i < 1000; i++)
            {
                var inputBool = new bool[i];

                for (var j = 0; j < i; j++)
                {
                    inputBool[j] = j % 2 == 0;
                }

                TestRoundTripValue(inputBool);
                TestRoundTripMember(inputBool);
            }
        }

        [Fact]
        public void TestFalseBooleanArray()
        {
            var inputBool = Enumerable.Repeat<bool>(false, 1000).ToArray();
            TestRoundTripValue(inputBool);
            TestRoundTripMember(inputBool);
        }

        private static readonly DateTime _testNow = DateTime.Now;

        [Fact]
        public void TestPrimitiveValues()
        {
            TestRoundTripValue(true);
            TestRoundTripValue(false);
            TestRoundTripValue(Byte.MaxValue);
            TestRoundTripValue(SByte.MaxValue);
            TestRoundTripValue(Int16.MaxValue);
            TestRoundTripValue(Int32.MaxValue);
            TestRoundTripValue(Byte.MaxValue);
            TestRoundTripValue(Int16.MaxValue);
            TestRoundTripValue(Int64.MaxValue);
            TestRoundTripValue(UInt16.MaxValue);
            TestRoundTripValue(UInt32.MaxValue);
            TestRoundTripValue(UInt64.MaxValue);
            TestRoundTripValue(Decimal.MaxValue);
            TestRoundTripValue(Double.MaxValue);
            TestRoundTripValue(Single.MaxValue);
            TestRoundTripValue('X');
            TestRoundTripValue("YYY");
            TestRoundTripValue("\uD800\uDC00"); // valid surrogate pair
            TestRoundTripValue("\uDC00\uD800"); // invalid surrogate pair
            TestRoundTripValue("\uD800"); // incomplete surrogate pair
            TestRoundTripValue<object>(null);
            TestRoundTripValue(ConsoleColor.Cyan);
            TestRoundTripValue(EByte.Value);
            TestRoundTripValue(ESByte.Value);
            TestRoundTripValue(EShort.Value);
            TestRoundTripValue(EUShort.Value);
            TestRoundTripValue(EInt.Value);
            TestRoundTripValue(EUInt.Value);
            TestRoundTripValue(ELong.Value);
            TestRoundTripValue(EULong.Value);
            TestRoundTripValue(_testNow);
        }

        [Fact]
        public void TestInt32Values()
        {
            TestRoundTripValue<Int32>(0);
            TestRoundTripValue<Int32>(1);
            TestRoundTripValue<Int32>(2);
            TestRoundTripValue<Int32>(3);
            TestRoundTripValue<Int32>(4);
            TestRoundTripValue<Int32>(5);
            TestRoundTripValue<Int32>(6);
            TestRoundTripValue<Int32>(7);
            TestRoundTripValue<Int32>(8);
            TestRoundTripValue<Int32>(9);
            TestRoundTripValue<Int32>(10);
            TestRoundTripValue<Int32>(-1);
            TestRoundTripValue<Int32>(Int32.MinValue);
            TestRoundTripValue<Int32>(Byte.MaxValue);
            TestRoundTripValue<Int32>(UInt16.MaxValue);
            TestRoundTripValue<Int32>(Int32.MaxValue);
        }

        [Fact]
        public void TestUInt32Values()
        {
            TestRoundTripValue<UInt32>(0);
            TestRoundTripValue<UInt32>(1);
            TestRoundTripValue<UInt32>(2);
            TestRoundTripValue<UInt32>(3);
            TestRoundTripValue<UInt32>(4);
            TestRoundTripValue<UInt32>(5);
            TestRoundTripValue<UInt32>(6);
            TestRoundTripValue<UInt32>(7);
            TestRoundTripValue<UInt32>(8);
            TestRoundTripValue<UInt32>(9);
            TestRoundTripValue<UInt32>(10);
            TestRoundTripValue<Int32>(Byte.MaxValue);
            TestRoundTripValue<Int32>(UInt16.MaxValue);
            TestRoundTripValue<Int32>(Int32.MaxValue);
        }

        [Fact]
        public void TestInt64Values()
        {
            TestRoundTripValue<Int64>(0);
            TestRoundTripValue<Int64>(1);
            TestRoundTripValue<Int64>(2);
            TestRoundTripValue<Int64>(3);
            TestRoundTripValue<Int64>(4);
            TestRoundTripValue<Int64>(5);
            TestRoundTripValue<Int64>(6);
            TestRoundTripValue<Int64>(7);
            TestRoundTripValue<Int64>(8);
            TestRoundTripValue<Int64>(9);
            TestRoundTripValue<Int64>(10);
            TestRoundTripValue<Int64>(-1);
            TestRoundTripValue<Int64>(Byte.MinValue);
            TestRoundTripValue<Int64>(Byte.MaxValue);
            TestRoundTripValue<Int64>(Int16.MinValue);
            TestRoundTripValue<Int64>(Int16.MaxValue);
            TestRoundTripValue<Int64>(UInt16.MinValue);
            TestRoundTripValue<Int64>(UInt16.MaxValue);
            TestRoundTripValue<Int64>(Int32.MinValue);
            TestRoundTripValue<Int64>(Int32.MaxValue);
            TestRoundTripValue<Int64>(UInt32.MinValue);
            TestRoundTripValue<Int64>(UInt32.MaxValue);
            TestRoundTripValue<Int64>(Int64.MinValue);
            TestRoundTripValue<Int64>(Int64.MaxValue);
        }

        [Fact]
        public void TestUInt64Values()
        {
            TestRoundTripValue<UInt64>(0);
            TestRoundTripValue<UInt64>(1);
            TestRoundTripValue<UInt64>(2);
            TestRoundTripValue<UInt64>(3);
            TestRoundTripValue<UInt64>(4);
            TestRoundTripValue<UInt64>(5);
            TestRoundTripValue<UInt64>(6);
            TestRoundTripValue<UInt64>(7);
            TestRoundTripValue<UInt64>(8);
            TestRoundTripValue<UInt64>(9);
            TestRoundTripValue<UInt64>(10);
            TestRoundTripValue<UInt64>(Byte.MinValue);
            TestRoundTripValue<UInt64>(Byte.MaxValue);
            TestRoundTripValue<UInt64>(UInt16.MinValue);
            TestRoundTripValue<UInt64>(UInt16.MaxValue);
            TestRoundTripValue<UInt64>(Int32.MaxValue);
            TestRoundTripValue<UInt64>(UInt32.MinValue);
            TestRoundTripValue<UInt64>(UInt32.MaxValue);
            TestRoundTripValue<UInt64>(UInt64.MinValue);
            TestRoundTripValue<UInt64>(UInt64.MaxValue);
        }

        [Fact]
        public void TestPrimitiveMemberValues()
        {
            TestRoundTripMember(true);
            TestRoundTripMember(false);
            TestRoundTripMember(Byte.MaxValue);
            TestRoundTripMember(SByte.MaxValue);
            TestRoundTripMember(Int16.MaxValue);
            TestRoundTripMember(Int32.MaxValue);
            TestRoundTripMember(Byte.MaxValue);
            TestRoundTripMember(Int16.MaxValue);
            TestRoundTripMember(Int64.MaxValue);
            TestRoundTripMember(UInt16.MaxValue);
            TestRoundTripMember(UInt32.MaxValue);
            TestRoundTripMember(UInt64.MaxValue);
            TestRoundTripMember(Decimal.MaxValue);
            TestRoundTripMember(Double.MaxValue);
            TestRoundTripMember(Single.MaxValue);
            TestRoundTripMember('X');
            TestRoundTripMember("YYY");
            TestRoundTripMember("\uD800\uDC00"); // valid surrogate pair
            TestRoundTripMember("\uDC00\uD800"); // invalid surrogate pair
            TestRoundTripMember("\uD800"); // incomplete surrogate pair
            TestRoundTripMember<object>(null);
            TestRoundTripMember(ConsoleColor.Cyan);
            TestRoundTripMember(EByte.Value);
            TestRoundTripMember(ESByte.Value);
            TestRoundTripMember(EShort.Value);
            TestRoundTripMember(EUShort.Value);
            TestRoundTripMember(EInt.Value);
            TestRoundTripMember(EUInt.Value);
            TestRoundTripMember(ELong.Value);
            TestRoundTripMember(EULong.Value);
            TestRoundTripMember(_testNow);
        }

        [Fact]
        public void TestPrimitiveAPIs()
        {
            TestRoundTrip(w => TestWritingPrimitiveAPIs(w), r => TestReadingPrimitiveAPIs(r));
        }

        [Fact]
        public void TestPrimitiveMemberAPIs()
        {
            TestRoundTrip(w => w.WriteValue(new PrimitiveMemberTest()), r => r.ReadValue());
        }

        public class PrimitiveMemberTest : IObjectWritable
        {
            public PrimitiveMemberTest()
            {
            }

            private PrimitiveMemberTest(ObjectReader reader)
            {
                TestReadingPrimitiveAPIs(reader);
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                TestWritingPrimitiveAPIs(writer);
            }

            static PrimitiveMemberTest()
            {
                ObjectBinder.RegisterTypeReader(typeof(PrimitiveMemberTest), r => new PrimitiveMemberTest(r));
            }
        }

        private static void TestWritingPrimitiveAPIs(ObjectWriter writer)
        {
            writer.WriteBoolean(true);
            writer.WriteBoolean(false);
            writer.WriteByte(Byte.MaxValue);
            writer.WriteSByte(SByte.MaxValue);
            writer.WriteInt16(Int16.MaxValue);
            writer.WriteInt32(Int32.MaxValue);
            writer.WriteInt32(Byte.MaxValue);
            writer.WriteInt32(Int16.MaxValue);
            writer.WriteInt64(Int64.MaxValue);
            writer.WriteUInt16(UInt16.MaxValue);
            writer.WriteUInt32(UInt32.MaxValue);
            writer.WriteUInt64(UInt64.MaxValue);
            writer.WriteDecimal(Decimal.MaxValue);
            writer.WriteDouble(Double.MaxValue);
            writer.WriteSingle(Single.MaxValue);
            writer.WriteChar('X');
            writer.WriteString("YYY");
            writer.WriteString("\uD800\uDC00"); // valid surrogate pair
            writer.WriteString("\uDC00\uD800"); // invalid surrogate pair
            writer.WriteString("\uD800"); // incomplete surrogate pair
        }

        private static void TestReadingPrimitiveAPIs(ObjectReader reader)
        {
            Assert.True(reader.ReadBoolean());
            Assert.False(reader.ReadBoolean());
            Assert.Equal(Byte.MaxValue, reader.ReadByte());
            Assert.Equal(SByte.MaxValue, reader.ReadSByte());
            Assert.Equal(Int16.MaxValue, reader.ReadInt16());
            Assert.Equal(Int32.MaxValue, reader.ReadInt32());
            Assert.Equal(Byte.MaxValue, reader.ReadInt32());
            Assert.Equal(Int16.MaxValue, reader.ReadInt32());
            Assert.Equal(Int64.MaxValue, reader.ReadInt64());
            Assert.Equal(UInt16.MaxValue, reader.ReadUInt16());
            Assert.Equal(UInt32.MaxValue, reader.ReadUInt32());
            Assert.Equal(UInt64.MaxValue, reader.ReadUInt64());
            Assert.Equal(Decimal.MaxValue, reader.ReadDecimal());
            Assert.Equal(Double.MaxValue, reader.ReadDouble());
            Assert.Equal(Single.MaxValue, reader.ReadSingle());
            Assert.Equal('X', reader.ReadChar());
            Assert.Equal("YYY", reader.ReadString());
            Assert.Equal("\uD800\uDC00", reader.ReadString()); // valid surrogate pair
            Assert.Equal("\uDC00\uD800", reader.ReadString()); // invalid surrogate pair
            Assert.Equal("\uD800", reader.ReadString()); // incomplete surrogate pair
        }

        [Fact]
        public void TestPrimitivesValue()
        {
            TestRoundTrip(w => TestWritingPrimitiveValues(w), r => TestReadingPrimitiveValues(r));
        }

        [Fact]
        public void TestPrimitiveValueAPIs()
        {
            TestRoundTrip(w => w.WriteValue(new PrimitiveValueTest()), r => r.ReadValue());
        }

        public class PrimitiveValueTest : IObjectWritable
        {
            public PrimitiveValueTest()
            {
            }

            private PrimitiveValueTest(ObjectReader reader)
            {
                TestReadingPrimitiveValues(reader);
            }

            bool IObjectWritable.ShouldReuseInSerialization => true;

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                TestWritingPrimitiveValues(writer);
            }

            static PrimitiveValueTest()
            {
                ObjectBinder.RegisterTypeReader(typeof(PrimitiveValueTest), r => new PrimitiveValueTest(r));
            }
        }

        private static void TestWritingPrimitiveValues(ObjectWriter writer)
        {
            writer.WriteValue(true);
            writer.WriteValue(false);
            writer.WriteValue(Byte.MaxValue);
            writer.WriteValue(SByte.MaxValue);
            writer.WriteValue(Int16.MaxValue);
            writer.WriteValue(Int32.MaxValue);
            writer.WriteValue((Int32)Byte.MaxValue);
            writer.WriteValue((Int32)Int16.MaxValue);
            writer.WriteValue(Int64.MaxValue);
            writer.WriteValue(UInt16.MaxValue);
            writer.WriteValue(UInt32.MaxValue);
            writer.WriteValue(UInt64.MaxValue);
            writer.WriteValue(Decimal.MaxValue);
            writer.WriteValue(Double.MaxValue);
            writer.WriteValue(Single.MaxValue);
            writer.WriteValue('X');
            writer.WriteValue("YYY");
            writer.WriteValue("\uD800\uDC00"); // valid surrogate pair
            writer.WriteValue("\uDC00\uD800"); // invalid surrogate pair
            writer.WriteValue("\uD800"); // incomplete surrogate pair
            writer.WriteValue((object)null);
            writer.WriteValue((IObjectWritable)null);
            unchecked
            {
                writer.WriteInt64((long)ConsoleColor.Cyan);
                writer.WriteInt64((long)EByte.Value);
                writer.WriteInt64((long)ESByte.Value);
                writer.WriteInt64((long)EShort.Value);
                writer.WriteInt64((long)EUShort.Value);
                writer.WriteInt64((long)EInt.Value);
                writer.WriteInt64((long)EUInt.Value);
                writer.WriteInt64((long)ELong.Value);
                writer.WriteInt64((long)EULong.Value);
            }
            writer.WriteValue(_testNow);
        }

        private static void TestReadingPrimitiveValues(ObjectReader reader)
        {
            Assert.True((bool)reader.ReadValue());
            Assert.False((bool)reader.ReadValue());
            Assert.Equal(Byte.MaxValue, (Byte)reader.ReadValue());
            Assert.Equal(SByte.MaxValue, (SByte)reader.ReadValue());
            Assert.Equal(Int16.MaxValue, (Int16)reader.ReadValue());
            Assert.Equal(Int32.MaxValue, (Int32)reader.ReadValue());
            Assert.Equal(Byte.MaxValue, (Int32)reader.ReadValue());
            Assert.Equal(Int16.MaxValue, (Int32)reader.ReadValue());
            Assert.Equal(Int64.MaxValue, (Int64)reader.ReadValue());
            Assert.Equal(UInt16.MaxValue, (UInt16)reader.ReadValue());
            Assert.Equal(UInt32.MaxValue, (UInt32)reader.ReadValue());
            Assert.Equal(UInt64.MaxValue, (UInt64)reader.ReadValue());
            Assert.Equal(Decimal.MaxValue, (Decimal)reader.ReadValue());
            Assert.Equal(Double.MaxValue, (Double)reader.ReadValue());
            Assert.Equal(Single.MaxValue, (Single)reader.ReadValue());
            Assert.Equal('X', (Char)reader.ReadValue());
            Assert.Equal("YYY", (String)reader.ReadValue());
            Assert.Equal("\uD800\uDC00", (String)reader.ReadValue()); // valid surrogate pair
            Assert.Equal("\uDC00\uD800", (String)reader.ReadValue()); // invalid surrogate pair
            Assert.Equal("\uD800", (String)reader.ReadValue()); // incomplete surrogate pair
            Assert.Null(reader.ReadValue());
            Assert.Null(reader.ReadValue());

            unchecked
            {
                Assert.Equal((long)ConsoleColor.Cyan, reader.ReadInt64());
                Assert.Equal((long)EByte.Value, reader.ReadInt64());
                Assert.Equal((long)ESByte.Value, reader.ReadInt64());
                Assert.Equal((long)EShort.Value, reader.ReadInt64());
                Assert.Equal((long)EUShort.Value, reader.ReadInt64());
                Assert.Equal((long)EInt.Value, reader.ReadInt64());
                Assert.Equal((long)EUInt.Value, reader.ReadInt64());
                Assert.Equal((long)ELong.Value, reader.ReadInt64());
                Assert.Equal((long)EULong.Value, reader.ReadInt64());
            }

            Assert.Equal(_testNow, (DateTime)reader.ReadValue());
        }

        public enum EByte : byte
        {
            Value = 1
        }

        public enum ESByte : sbyte
        {
            Value = 2
        }

        public enum EShort : short
        {
            Value = 3
        }

        public enum EUShort : ushort
        {
            Value = 4
        }

        public enum EInt : int
        {
            Value = 5
        }

        public enum EUInt : uint
        {
            Value = 6
        }

        public enum ELong : long
        {
            Value = 7
        }

        public enum EULong : ulong
        {
            Value = 8
        }

        [Fact]
        public void TestRoundTripCharacters()
        {
            // round trip all possible characters as a string
            for (int i = ushort.MinValue; i <= ushort.MaxValue; i++)
            {
                TestRoundTripChar((char)i);
            }
        }

        private void TestRoundTripChar(Char ch)
        {
            TestRoundTrip(ch, (w, v) => w.WriteChar(v), r => r.ReadChar());
        }

        [Fact]
        public void TestRoundTripGuid()
        {
            void test(Guid guid)
            {
                TestRoundTrip(guid, (w, v) => w.WriteGuid(v), r => r.ReadGuid());
            }

            test(Guid.Empty);
            test(new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1));
            test(new Guid(0b10000000000000000000000000000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1));
            test(new Guid(0b10000000000000000000000000000000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            for (int i = 0; i < 10; i++)
            {
                test(Guid.NewGuid());
            }
        }

        [Fact]
        public void TestRoundTripStringCharacters()
        {
            // round trip all possible characters as a string
            for (int i = ushort.MinValue; i <= ushort.MaxValue; i++)
            {
                TestRoundTripStringCharacter((ushort)i);
            }

            // round trip single string with all possible characters
            var sb = new StringBuilder();
            for (int i = ushort.MinValue; i <= ushort.MaxValue; i++)
            {
                sb.Append((char)i);
            }

            TestRoundTripString(sb.ToString());
        }

        private void TestRoundTripString(string text)
        {
            TestRoundTrip(text, (w, v) => w.WriteString(v), r => r.ReadString());
        }

        private void TestRoundTripStringCharacter(ushort code)
        {
            TestRoundTripString(new String((char)code, 1));
        }

        [Fact]
        public void TestRoundTripArrays()
        {
            //TestRoundTripArray(new object[] { });
            //TestRoundTripArray(new object[] { "hello" });
            //TestRoundTripArray(new object[] { "hello", "world" });
            //TestRoundTripArray(new object[] { "hello", "world", "good" });
            //TestRoundTripArray(new object[] { "hello", "world", "good", "bye" });
            //TestRoundTripArray(new object[] { "hello", 123, 45m, 99.9, 'c' });
            TestRoundTripArray(new string[] { "hello", null, "world" });
        }

        private void TestRoundTripArray<T>(T[] values)
        {
            TestRoundTripValue(values);
        }

        [Fact]
        public void TestObjectMapLimits()
        {
            using (var stream = new MemoryStream())
            {
                var instances = new List<TypeWithTwoMembers<int, string>>();

                // We need enough items to exercise all sizes of ObjectRef
                for (int i = 0; i < ushort.MaxValue + 1; i++)
                {
                    instances.Add(new TypeWithTwoMembers<int, string>(i, i.ToString()));
                }

                var writer = new ObjectWriter(stream);
                // Write each instance twice. The second time around, they'll become ObjectRefs
                for (int pass = 0; pass < 2; pass++)
                {
                    foreach (var instance in instances)
                    {
                        writer.WriteValue(instance);
                    }
                }

                writer.Dispose();

                stream.Position = 0;
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    for (int pass = 0; pass < 2; pass++)
                    {
                        foreach (var instance in instances)
                        {
                            var obj = reader.ReadValue();
                            Assert.NotNull(obj);
                            Assert.True(Equalish(obj, instance));
                        }
                    }
                }
            }
        }

        [Fact]
        public void TestObjectGraph()
        {
            var oneNode = new Node("one");
            TestRoundTripValue(oneNode);
            TestRoundTripValue(new Node("a", new Node("b"), new Node("c")));
            TestRoundTripValue(new Node("x", oneNode, oneNode, oneNode, oneNode));
        }

        [Fact]
        public void TestReuse()
        {
            var oneNode = new Node("one");
            var n1 = new Node("x", oneNode, oneNode, oneNode, oneNode);
            var n2 = RoundTripValue(n1, recursive: true);

            Assert.Same(n2.Children[0], n2.Children[1]);
            Assert.Same(n2.Children[1], n2.Children[2]);
            Assert.Same(n2.Children[2], n2.Children[3]);
        }

        [Fact]
        public void TestReuseNegative()
        {
            var oneNode = new Node("one", isReusable: false);
            var n1 = new Node("x", oneNode, oneNode, oneNode, oneNode);
            var n2 = RoundTripValue(n1, recursive: true);

            Assert.NotSame(n2.Children[0], n2.Children[1]);
            Assert.NotSame(n2.Children[1], n2.Children[2]);
            Assert.NotSame(n2.Children[2], n2.Children[3]);
        }

        [Fact]
        public void TestWideObjectGraph()
        {
            int id = 0;
            var graph = ConstructGraph(ref id, 5, 3);
            TestRoundTripValue(graph);
        }

        [Fact]
        public void TestDeepObjectGraph_RecursiveSucceeds()
        {
            int id = 0;
            var graph = ConstructGraph(ref id, 1, 1000);
            TestRoundTripValue(graph);
        }

        [Fact]
        public void TestDeepObjectGraph_NonRecursiveSucceeds()
        {
            int id = 0;
            var graph = ConstructGraph(ref id, 1, 1000);
            TestRoundTripValue(graph, recursive: false);
        }

        private Node ConstructGraph(ref int id, int width, int depth)
        {
            var name = "node" + (id++);

            Node[] children;

            if (depth > 0)
            {
                children = new Node[width];

                for (int i = 0; i < width; i++)
                {
                    children[i] = ConstructGraph(ref id, width, depth - 1);
                }
            }
            else
            {
                children = Array.Empty<Node>();
            }

            return new Node(name, children);
        }

        private class Node : IObjectWritable, IEquatable<Node>
        {
            internal readonly string Name;
            internal readonly Node[] Children;
            private readonly bool _isReusable = true;

            public Node(string name, params Node[] children)
            {
                this.Name = name;
                this.Children = children;
            }

            public Node(string name, bool isReusable)
                : this(name)
            {
                this._isReusable = isReusable;
            }

            private Node(ObjectReader reader)
            {
                this.Name = reader.ReadString();
                this.Children = (Node[])reader.ReadValue();
            }

            private static readonly Func<ObjectReader, object> s_createInstance = r => new Node(r);

            bool IObjectWritable.ShouldReuseInSerialization => _isReusable;

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteString(this.Name);
                writer.WriteValue(this.Children);
            }

            static Node()
            {
                ObjectBinder.RegisterTypeReader(typeof(Node), r => new Node(r));
            }

            public override Int32 GetHashCode()
            {
                return this.Name != null ? this.Name.GetHashCode() : 0;
            }

            public override Boolean Equals(Object obj)
            {
                return Equals(obj as Node);
            }

            public bool Equals(Node node)
            {
                if (node == null || this.Name != node.Name)
                {
                    return false;
                }

                if (this.Children.Length != node.Children.Length)
                {
                    return false;
                }

                for (int i = 0; i < this.Children.Length; i++)
                {
                    if (!this.Children[i].Equals(node.Children[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        // keep these around for analyzing perf issues
#if false
        [Fact]
        public void TestReaderPerf()
        {
            var iterations = 10000;

            var recTime = TestReaderPerf(iterations, recursive: true);
            var nonTime = TestReaderPerf(iterations, recursive: false);

            Console.WriteLine($"Recursive Time    : {recTime.TotalMilliseconds}");
            Console.WriteLine($"Non Recursive Time: {nonTime.TotalMilliseconds}");
        }

        [Fact]
        public void TestNonRecursiveReaderPerf()
        {
            var iterations = 10000;
            var nonTime = TestReaderPerf(iterations, recursive: false);
        }

        private TimeSpan TestReaderPerf(int iterations, bool recursive)
        {
            int id = 0;
            var graph = ConstructGraph(ref id, 5, 3);

            var stream = new MemoryStream();
            var binder = new RecordingObjectBinder();
            var writer = new StreamObjectWriter(stream, binder: binder, recursive: recursive);

            writer.WriteValue(graph);
            writer.Dispose();

            var start = DateTime.Now;
            for (int i = 0; i < iterations; i++)
            {
                stream.Position = 0;
                var reader = new StreamObjectReader(stream, binder: binder);
                var item = reader.ReadValue();
            }

            return DateTime.Now - start;
        }
#endif
    }
}
