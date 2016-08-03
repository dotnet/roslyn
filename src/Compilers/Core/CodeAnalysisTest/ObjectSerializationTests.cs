// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class ObjectSerializationTests
    {
        [Fact]
        public void TestRoundTripPrimitiveArray()
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

            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);

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

            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
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

            reader.Dispose();
        }

        [Fact]
        public void TestRoundTripBooleanArray()
        {
            for (var i = 0; i < 1000; i++)
            {
                var inputBool = new bool[i];

                for (var j = 0; j < i; j++)
                {
                    inputBool[j] = j % 2 == 0;
                }

                var stream = new MemoryStream();
                var writer = new ObjectWriter(stream);

                writer.WriteValue(inputBool);

                writer.Dispose();

                stream.Position = 0;
                var reader = new ObjectReader(stream);
                Assert.True(Enumerable.SequenceEqual(inputBool, (bool[])reader.ReadValue()));

                reader.Dispose();
            }
        }

        [Fact]
        public void TestRoundTripFalseBooleanArray()
        {
            var inputBool = Enumerable.Repeat<bool>(false, 1000).ToArray();

            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);

            writer.WriteValue(inputBool);

            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
            Assert.True(Enumerable.SequenceEqual(inputBool, (bool[])reader.ReadValue()));

            reader.Dispose();
        }

        [Fact]
        public void TestRoundTripPrimitives()
        {
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
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
            writer.WriteCompressedUInt(Byte.MaxValue >> 2);   // 6 bits
            writer.WriteCompressedUInt(UInt16.MaxValue >> 2); // 14 bits
            writer.WriteCompressedUInt(UInt32.MaxValue >> 2); // 30 bits
            var dt = DateTime.Now;
            writer.WriteDateTime(dt);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
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
            Assert.Equal((UInt32)(Byte.MaxValue >> 2), reader.ReadCompressedUInt());
            Assert.Equal((UInt32)(UInt16.MaxValue >> 2), reader.ReadCompressedUInt());
            Assert.Equal(UInt32.MaxValue >> 2, reader.ReadCompressedUInt());
            Assert.Equal(dt, reader.ReadDateTime());
            reader.Dispose();
        }

        [Fact]
        public void TestRoundTripPrimitivesAsValues()
        {
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
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
            writer.WriteString("\uD800\uDC00"); // valid surrogate pair
            writer.WriteString("\uDC00\uD800"); // invalid surrogate pair
            writer.WriteString("\uD800"); // incomplete surrogate pair
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
            var date = DateTime.Now;
            writer.WriteValue(date);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream, binder: writer.Binder);
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
            Assert.Equal(date, (DateTime)reader.ReadValue());
            reader.Dispose();
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
                RoundTripCharacter((char)i);
            }
        }

        private void RoundTripCharacter(Char ch)
        {
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteChar(ch);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
            var readch = reader.ReadChar();
            reader.Dispose();

            Assert.Equal(ch, readch);
        }

        [Fact]
        public void TestRoundTripStringCharacters()
        {
            // round trip all possible characters as a string
            for (int i = ushort.MinValue; i <= ushort.MaxValue; i++)
            {
                RoundTripStringCharacter((ushort)i);
            }

            // round trip single string with all possible characters
            var sb = new StringBuilder();
            for (int i = ushort.MinValue; i <= ushort.MaxValue; i++)
            {
                sb.Append((char)i);
            }

            RoundTripString(sb.ToString());
        }

        private void RoundTripString(string text)
        {
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteString(text);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
            var readText = reader.ReadString();
            reader.Dispose();

            Assert.Equal(text, readText);
        }

        private void RoundTripStringCharacter(ushort code)
        {
            RoundTripString(new String((char)code, 1));
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
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteValue(values);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream, binder: writer.Binder);
            var readValues = (T[])reader.ReadValue();
            reader.Dispose();

            Assert.Equal(true, values.SequenceEqual(readValues));
        }

        [Fact]
        public void TestRoundTripWritableObject()
        {
            var instance = new WritableClass(123, "456");

            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteValue(instance);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream, binder: writer.Binder);
            var instance2 = (WritableClass)reader.ReadValue();
            reader.Dispose();

            Assert.NotNull(instance2);
            Assert.Equal(instance.X, instance2.X);
            Assert.Equal(instance.Y, instance2.Y);
        }

        [Fact]
        public void TestObjectMapLimits()
        {
            using (var stream = new MemoryStream())
            {
                var instances = new List<WritableClass>();

                // We need enough items to exercise all sizes of ObjectRef
                for (int i = 0; i < ushort.MaxValue + 1; i++)
                {
                    instances.Add(new WritableClass(i, i.ToString()));
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

                var binder = writer.Binder;
                writer.Dispose();

                stream.Position = 0;
                using (var reader = new ObjectReader(stream, binder: binder))
                {
                    for (int pass = 0; pass < 2; pass++)
                    {
                        foreach (var instance in instances)
                        {
                            var obj = (WritableClass)reader.ReadValue();
                            Assert.NotNull(obj);
                            Assert.Equal(obj.X, instance.X);
                            Assert.Equal(obj.Y, instance.Y);
                        }
                    }
                }
            }
        }

        private class WritableClass : IObjectWritable, IObjectReadable
        {
            internal readonly int X;
            internal readonly string Y;

            public WritableClass(int x, string y)
            {
                this.X = x;
                this.Y = y;
            }

            private WritableClass(ObjectReader reader)
            {
                this.X = reader.ReadInt32();
                this.Y = reader.ReadString();
            }

            public void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(this.X);
                writer.WriteString(this.Y);
            }

            Func<ObjectReader, object> IObjectReadable.GetReader()
            {
                return (r) => new WritableClass(r);
            }
        }

#if false
        [Fact]
        public void TestRoundTripSerializableObject()
        {
            var instance = new SerializableClass(123, "456");

            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteValue(instance);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
            var instance2 = (SerializableClass)reader.ReadValue();
            reader.Dispose();

            Assert.NotNull(instance2);
            Assert.Equal(instance.X, instance2.X);
            Assert.Equal(instance.Y, instance2.Y);
        }

        private class SerializableClass : ISerializable
        {
            internal readonly int X;
            internal readonly string Y;

            public SerializableClass(int x, string y)
            {
                this.X = x;
                this.Y = y;
            }

            private SerializableClass(SerializationInfo info, StreamingContext context)
            {
                this.X = info.GetInt32("x");
                this.Y = info.GetString("y");
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("x", this.X);
                info.AddValue("y", this.Y);
            }
        }

        [Fact]
        public void TestRoundTripLocation()
        {
            var instance = new SpecialLocation();
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteValue(instance);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
            var instance2 = (Location)reader.ReadValue();
            reader.Dispose();

            Assert.NotNull(instance2);
            Assert.Equal(instance2.GetType(), typeof(SerializedLocation));
            Assert.Equal(instance.Kind, instance2.Kind);
            Assert.Equal(instance.IsInSource, instance2.IsInSource);
            Assert.Equal(instance.SourceSpan, instance2.SourceSpan);
            Assert.Equal(instance.Kind, instance2.Kind);
        }

        private class SpecialLocation : Location
        {
            private TextSpan textSpan = new TextSpan(10, 20);

            public override LocationKind Kind
            {
                get
                {
                    return LocationKind.None;
                }
            }

            public override TextSpan SourceSpan
            {
                get
                {
                    return textSpan;
                }
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }

        [Fact]
        public void TestRoundTripRawSerializableObject()
        {
            var instance = new RawSerializableClass(123, "456");

            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteValue(instance);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
            var instance2 = (RawSerializableClass)reader.ReadValue();
            reader.Dispose();

            Assert.NotNull(instance2);
            Assert.Equal(instance.X, instance2.X);
            Assert.Equal(instance.Y, instance2.Y);
        }

        [Serializable]
        private class RawSerializableClass
        {
            internal readonly int X;
            internal readonly string Y;

            public RawSerializableClass(int x, string y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [Fact]
        public void TestRoundTripSingletonClass()
        {
            var instance = SingletonClass.Singleton;

            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteValue(instance);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream);
            var instance2 = (SingletonClass)reader.ReadValue();
            reader.Dispose();

            Assert.Same(instance, instance2);
        }

        [Serializable]
        private class SingletonClass : IObjectReference
        {
            internal readonly int X;
            internal readonly string Y;

            private SingletonClass(int x, string y)
            {
                this.X = x;
                this.Y = y;
            }

            public static readonly SingletonClass Singleton = new SingletonClass(999, "999");

            public object GetRealObject(StreamingContext context)
            {
                return Singleton;
            }
        }
#endif

        [Fact]
        public void TestRoundTripGraph()
        {
            var oneNode = new Node("one");
            TestRoundTripGraphCore(oneNode);

            TestRoundTripGraphCore(new Node("a", new Node("b"), new Node("c")));
            TestRoundTripGraphCore(new Node("x", oneNode, oneNode, oneNode, oneNode));
#if false  // cycles not supported
            var cyclicNode = new Node("cyclic", oneNode);
            cyclicNode.Children[0] = cyclicNode;
            TestRoundTripGraph(cyclicNode);
#endif
        }

        private void TestRoundTripGraphCore(Node graph)
        {
            var stream = new MemoryStream();
            var writer = new ObjectWriter(stream);
            writer.WriteValue(graph);
            writer.Dispose();

            stream.Position = 0;
            var reader = new ObjectReader(stream, binder: writer.Binder);
            var newGraph = (Node)reader.ReadValue();
            reader.Dispose();

            Assert.NotNull(newGraph);
            Assert.Equal(true, graph.IsEquivalentTo(newGraph));
        }

        private class Node : IObjectWritable, IObjectReadable
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

            Func<ObjectReader, object> IObjectReadable.GetReader()
            {
                return (r) => new Node(r);
            }

            public bool IsEquivalentTo(Node node)
            {
                if (this.Name != node.Name)
                {
                    return false;
                }

                if (this.Children.Length != node.Children.Length)
                {
                    return false;
                }

                for (int i = 0; i < this.Children.Length; i++)
                {
                    if (!this.Children[i].IsEquivalentTo(node.Children[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
