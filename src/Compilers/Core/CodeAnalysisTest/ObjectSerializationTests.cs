// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using System.Collections;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class ObjectSerializationTests
    {
        private void TestRoundTrip(Action<ObjectWriter> writeAction, Action<ObjectReader> readAction)
        {
            var stream = new MemoryStream();
            var writer = new StreamObjectWriter(stream);

            writeAction(writer);
            writer.Dispose();

            stream.Position = 0;
            using (var reader = new StreamObjectReader(stream, binder: writer.Binder))
            {
                readAction(reader);
            }
        }

        private T RoundTrip<T>(T value, Action<ObjectWriter, T> writeAction, Func<ObjectReader, T> readAction)
        {
            var stream = new MemoryStream();
            var writer = new StreamObjectWriter(stream);

            writeAction(writer, value);
            writer.Dispose();

            stream.Position = 0;
            using (var reader = new StreamObjectReader(stream, binder: writer.Binder))
            {
                return (T)readAction(reader);
            }
        }

        private void TestRoundTrip<T>(T value, Action<ObjectWriter, T> writeAction, Func<ObjectReader, T> readAction)
        {
            var newValue = RoundTrip(value, writeAction, readAction);
            Assert.True(Equalish(value, newValue));
        }

        private T RoundTripValue<T>(T value)
        {
            return RoundTrip(value, (w, v) => w.WriteValue(v), r => (T)r.ReadValue());
        }

        private void TestRoundTripValue<T>(T value)
        {
            var newValue = RoundTripValue(value);
            Assert.True(Equalish(value, newValue));
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

        private class TypeWithOneMember<T> : IObjectWritable, IObjectReadable, IEquatable<TypeWithOneMember<T>>
        {
            private T _member;

            public TypeWithOneMember(T value)
            {
                _member = value;
            }

            private TypeWithOneMember(ObjectReader reader)
            {
                _member = (T)reader.ReadValue();
            }

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                writer.WriteValue(_member);
            }

            Func<ObjectReader, object> IObjectReadable.GetReader() => (r) => new TypeWithOneMember<T>(r);

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

        private class TypeWithTwoMembers<T, S> : IObjectWritable, IObjectReadable, IEquatable<TypeWithTwoMembers<T, S>>
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

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                writer.WriteValue(_member1);
                writer.WriteValue(_member2);
            }

            Func<ObjectReader, object> IObjectReadable.GetReader() => (r) => new TypeWithTwoMembers<T, S>(r);

            public override int GetHashCode()
            {
                if (_member1 == null)
                {
                    return 0;
                }
                else
                {
                    return  _member1.GetHashCode();
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
        private class TypeWithManyMembers<T> : IObjectWritable, IObjectReadable, IEquatable<TypeWithManyMembers<T>>
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

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(_members.Length);

                for (int i = 0; i < _members.Length; i++)
                {
                    writer.WriteValue(_members[i]);
                }
            }

            Func<ObjectReader, object> IObjectReadable.GetReader() => (r) => new TypeWithManyMembers<T>(r);

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
        public void TestPrimitiveArrayValues()
        {
            TestRoundTrip(w => TestWritingPrimitiveArrays(w), r => TestReadingPrimitiveArrays(r));
        }

        [Fact]
        public void TestPrimitiveArrayMembers()
        {
            TestRoundTrip(w => w.WriteValue(new PrimitiveArrayMemberTest()), r => r.ReadValue());
        }

        public class PrimitiveArrayMemberTest : IObjectWritable, IObjectReadable
        {
            public PrimitiveArrayMemberTest()
            {
            }

            private PrimitiveArrayMemberTest(ObjectReader reader)
            {
                TestReadingPrimitiveArrays(reader);
            }

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                TestWritingPrimitiveArrays(writer);
            }

            Func<ObjectReader, object> IObjectReadable.GetReader() => (r) => new PrimitiveArrayMemberTest(r);
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
            TestRoundTripValue(DateTime.Now);
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
            TestRoundTripValue(typeof(object));
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
            TestRoundTripMember(DateTime.Now);
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
            TestRoundTripMember(typeof(object));
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

        public class PrimitiveMemberTest : IObjectWritable, IObjectReadable
        {
            public PrimitiveMemberTest()
            {
            }

            private PrimitiveMemberTest(ObjectReader reader)
            {
                TestReadingPrimitiveAPIs(reader);
            }

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                TestWritingPrimitiveAPIs(writer);
            }

            Func<ObjectReader, object> IObjectReadable.GetReader() => (r) => new PrimitiveMemberTest(r);
        }

        private static readonly DateTime _testNow = DateTime.Now;

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
            writer.WriteDateTime(_testNow);
        }

        private static void TestReadingPrimitiveAPIs(ObjectReader reader)
        {
            Assert.Equal(true, reader.ReadBoolean());
            Assert.Equal(false, reader.ReadBoolean());
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
            Assert.Equal(_testNow, reader.ReadDateTime());
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

        public class PrimitiveValueTest : IObjectWritable, IObjectReadable
        {
            public PrimitiveValueTest()
            {
            }

            private PrimitiveValueTest(ObjectReader reader)
            {
                TestReadingPrimitiveValues(reader);
            }

            void IObjectWritable.WriteTo(ObjectWriter writer)
            {
                TestWritingPrimitiveValues(writer);
            }

            Func<ObjectReader, object> IObjectReadable.GetReader() => (r) => new PrimitiveValueTest(r);
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
            writer.WriteValue(null);
            writer.WriteValue(ConsoleColor.Cyan);
            writer.WriteValue(EByte.Value);
            writer.WriteValue(ESByte.Value);
            writer.WriteValue(EShort.Value);
            writer.WriteValue(EUShort.Value);
            writer.WriteValue(EInt.Value);
            writer.WriteValue(EUInt.Value);
            writer.WriteValue(ELong.Value);
            writer.WriteValue(EULong.Value);
            writer.WriteValue(typeof(object));
            writer.WriteValue(_testNow);
        }

        private static void TestReadingPrimitiveValues(ObjectReader reader)
        {
            Assert.Equal(true, (bool)reader.ReadValue());
            Assert.Equal(false, (bool)reader.ReadValue());
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
            Assert.Equal(null, reader.ReadValue());
            Assert.Equal(ConsoleColor.Cyan, reader.ReadValue());
            Assert.Equal(EByte.Value, reader.ReadValue());
            Assert.Equal(ESByte.Value, reader.ReadValue());
            Assert.Equal(EShort.Value, reader.ReadValue());
            Assert.Equal(EUShort.Value, reader.ReadValue());
            Assert.Equal(EInt.Value, reader.ReadValue());
            Assert.Equal(EUInt.Value, reader.ReadValue());
            Assert.Equal(ELong.Value, reader.ReadValue());
            Assert.Equal(EULong.Value, reader.ReadValue());
            Assert.Equal(typeof(object), (Type)reader.ReadValue());
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
            TestRoundTripArray(new object[] { });
            TestRoundTripArray(new object[] { "hello" });
            TestRoundTripArray(new object[] { "hello", "world" });
            TestRoundTripArray(new object[] { "hello", "world", "good" });
            TestRoundTripArray(new object[] { "hello", "world", "good", "bye" });
            TestRoundTripArray(new object[] { "hello", 123, 45m, 99.9, 'c' });
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

                var writer = new StreamObjectWriter(stream);
                // Write each instance twice. The second time around, they'll become ObjectRefs
                for (int pass = 0; pass < 2; pass++)
                {
                    foreach (var instance in instances)
                    {
                        writer.WriteValue(instance);
                    }
                }

                var binder = writer.Binder;
                writer.Dispose();

                stream.Position = 0;
                using (var reader = new StreamObjectReader(stream, binder: binder))
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
        public void TestRoundTripGraph()
        {
            var oneNode = new Node("one");
            TestRoundTripValue(oneNode);

            TestRoundTripValue(new Node("a", new Node("b"), new Node("c")));
            TestRoundTripValue(new Node("x", oneNode, oneNode, oneNode, oneNode));
        }

        private class Node : IObjectWritable, IObjectReadable, IEquatable<Node>
        {
            internal readonly string Name;
            internal readonly Node[] Children;

            public Node(string name, params Node[] children)
            {
                this.Name = name;
                this.Children = children;
            }

            private Node(ObjectReader reader)
            {
                this.Name = reader.ReadString();
                this.Children = (Node[])reader.ReadValue();
            }

            private static readonly Func<ObjectReader, object> s_createInstance = r => new Node(r);

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteString(this.Name);
                writer.WriteValue(this.Children);
            }

            Func<ObjectReader, object> IObjectReadable.GetReader() => (r) => new Node(r);

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
    }
}
