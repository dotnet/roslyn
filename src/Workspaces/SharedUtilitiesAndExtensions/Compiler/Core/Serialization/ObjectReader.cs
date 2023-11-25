// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

#if COMPILERCORE
using Resources = CodeAnalysisResources;
#elif CODE_STYLE
using Resources = CodeStyleResources;
#else
using Resources = WorkspacesResources;
#endif

using TypeCode = ObjectWriter.TypeCode;

/// <summary>
/// An <see cref="ObjectReader"/> that deserializes objects from a byte stream.
/// </summary>
internal sealed partial class ObjectReader : IDisposable
{
    /// <summary>
    /// We start the version at something reasonably random.  That way an older file, with 
    /// some random start-bytes, has little chance of matching our version.  When incrementing
    /// this version, just change VersionByte2.
    /// </summary>
    internal const byte VersionByte1 = 0b10101010;
    internal const byte VersionByte2 = 0b00001100;

    private readonly BinaryReader _reader;
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Map of reference id's to deserialized strings.
    /// </summary>
    private readonly ReaderReferenceMap<string> _stringReferenceMap;

    /// <summary>
    /// Creates a new instance of a <see cref="ObjectReader"/>.
    /// </summary>
    /// <param name="stream">The stream to read objects from.</param>
    /// <param name="leaveOpen">True to leave the <paramref name="stream"/> open after the <see cref="ObjectWriter"/> is disposed.</param>
    /// <param name="cancellationToken"></param>
    private ObjectReader(
        Stream stream,
        bool leaveOpen,
        CancellationToken cancellationToken)
    {
        // String serialization assumes both reader and writer to be of the same endianness.
        // It can be adjusted for BigEndian if needed.
        Debug.Assert(BitConverter.IsLittleEndian);

        _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
        _stringReferenceMap = ReaderReferenceMap<string>.Create();

        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Attempts to create a <see cref="ObjectReader"/> from the provided <paramref name="stream"/>.
    /// If the <paramref name="stream"/> does not start with a valid header, then <see langword="null"/> will
    /// be returned.
    /// </summary>
    public static ObjectReader TryGetReader(
        Stream stream,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        if (stream == null)
        {
            return null;
        }

        try
        {
            if (stream.ReadByte() != VersionByte1 ||
                stream.ReadByte() != VersionByte2)
            {
                return null;
            }
        }
        catch (AggregateException ex) when (ex.InnerException is not null)
        {
            // PipeReaderStream wraps any exception it throws in an AggregateException, which is not expected by
            // callers treating it as a normal stream. Unwrap and rethrow the inner exception for clarity.
            // https://github.com/dotnet/runtime/issues/70206
#if NETCOREAPP
            ExceptionDispatchInfo.Throw(ex.InnerException);
#else
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
#endif
        }

        return new ObjectReader(stream, leaveOpen, cancellationToken);
    }

    /// <summary>
    /// Creates an <see cref="ObjectReader"/> from the provided <paramref name="stream"/>.
    /// Unlike <see cref="TryGetReader(Stream, bool, CancellationToken)"/>, it requires the version
    /// of the data in the stream to exactly match the current format version.
    /// Should only be used to read data written by the same version of Roslyn.
    /// </summary>
    public static ObjectReader GetReader(
        Stream stream,
        bool leaveOpen,
        CancellationToken cancellationToken)
    {
        var b = stream.ReadByte();
        if (b == -1)
        {
            throw new EndOfStreamException();
        }

        if (b != VersionByte1)
        {
            throw ExceptionUtilities.UnexpectedValue(b);
        }

        b = stream.ReadByte();
        if (b == -1)
        {
            throw new EndOfStreamException();
        }

        if (b != VersionByte2)
        {
            throw ExceptionUtilities.UnexpectedValue(b);
        }

        return new ObjectReader(stream, leaveOpen, cancellationToken);
    }

    public void Dispose()
    {
        _stringReferenceMap.Dispose();
    }

    public bool ReadBoolean() => _reader.ReadBoolean();
    public byte ReadByte() => _reader.ReadByte();
    // read as ushort because BinaryWriter fails on chars that are unicode surrogates
    public char ReadChar() => (char)_reader.ReadUInt16();
    public decimal ReadDecimal() => _reader.ReadDecimal();
    public double ReadDouble() => _reader.ReadDouble();
    public float ReadSingle() => _reader.ReadSingle();
    public int ReadInt32() => _reader.ReadInt32();
    public long ReadInt64() => _reader.ReadInt64();
    public sbyte ReadSByte() => _reader.ReadSByte();
    public short ReadInt16() => _reader.ReadInt16();
    public uint ReadUInt32() => _reader.ReadUInt32();
    public ulong ReadUInt64() => _reader.ReadUInt64();
    public ushort ReadUInt16() => _reader.ReadUInt16();
    public string ReadString() => ReadStringValue();

    public Guid ReadGuid()
    {
        var accessor = new ObjectWriter.GuidAccessor
        {
            Low64 = ReadInt64(),
            High64 = ReadInt64()
        };

        return accessor.Guid;
    }

    public object ReadValue()
    {
        var code = (TypeCode)_reader.ReadByte();
        switch (code)
        {
            case TypeCode.Null: return null;
            case TypeCode.Boolean_True: return true;
            case TypeCode.Boolean_False: return false;
            case TypeCode.Int8: return _reader.ReadSByte();
            case TypeCode.UInt8: return _reader.ReadByte();
            case TypeCode.Int16: return _reader.ReadInt16();
            case TypeCode.UInt16: return _reader.ReadUInt16();
            case TypeCode.Int32: return _reader.ReadInt32();
            case TypeCode.Int32_1Byte: return (int)_reader.ReadByte();
            case TypeCode.Int32_2Bytes: return (int)_reader.ReadUInt16();
            case TypeCode.Int32_0:
            case TypeCode.Int32_1:
            case TypeCode.Int32_2:
            case TypeCode.Int32_3:
            case TypeCode.Int32_4:
            case TypeCode.Int32_5:
            case TypeCode.Int32_6:
            case TypeCode.Int32_7:
            case TypeCode.Int32_8:
            case TypeCode.Int32_9:
            case TypeCode.Int32_10:
                return (int)code - (int)TypeCode.Int32_0;
            case TypeCode.UInt32: return _reader.ReadUInt32();
            case TypeCode.UInt32_1Byte: return (uint)_reader.ReadByte();
            case TypeCode.UInt32_2Bytes: return (uint)_reader.ReadUInt16();
            case TypeCode.UInt32_0:
            case TypeCode.UInt32_1:
            case TypeCode.UInt32_2:
            case TypeCode.UInt32_3:
            case TypeCode.UInt32_4:
            case TypeCode.UInt32_5:
            case TypeCode.UInt32_6:
            case TypeCode.UInt32_7:
            case TypeCode.UInt32_8:
            case TypeCode.UInt32_9:
            case TypeCode.UInt32_10:
                return (uint)((int)code - (int)TypeCode.UInt32_0);
            case TypeCode.Int64: return _reader.ReadInt64();
            case TypeCode.UInt64: return _reader.ReadUInt64();
            case TypeCode.Float4: return _reader.ReadSingle();
            case TypeCode.Float8: return _reader.ReadDouble();
            case TypeCode.Decimal: return _reader.ReadDecimal();
            case TypeCode.Char:
                // read as ushort because BinaryWriter fails on chars that are unicode surrogates
                return (char)_reader.ReadUInt16();
            case TypeCode.StringUtf8:
            case TypeCode.StringUtf16:
            case TypeCode.StringRef_4Bytes:
            case TypeCode.StringRef_1Byte:
            case TypeCode.StringRef_2Bytes:
                return ReadStringValue(code);
            case TypeCode.DateTime:
                return DateTime.FromBinary(_reader.ReadInt64());
            case TypeCode.Array:
            case TypeCode.Array_0:
            case TypeCode.Array_1:
            case TypeCode.Array_2:
            case TypeCode.Array_3:
                return ReadArray(code);

            case TypeCode.EncodingName:
                return Encoding.GetEncoding(ReadString());

            case >= TypeCode.FirstWellKnownTextEncoding and <= TypeCode.LastWellKnownTextEncoding:
                return ObjectWriter.ToEncodingKind(code).GetEncoding();

            case TypeCode.EncodingCodePage:
                return Encoding.GetEncoding(ReadInt32());

            default:
                throw ExceptionUtilities.UnexpectedValue(code);
        }
    }

    public (char[] array, int length) ReadCharArray(Func<int, char[]> getArray)
    {
        var kind = (TypeCode)_reader.ReadByte();

        (var length, _) = ReadArrayLengthAndElementKind(kind);
        var array = getArray(length);

        var charsRead = _reader.Read(array, 0, length);

        return (array, charsRead);
    }

    /// <summary>
    /// A reference-id to object map, that can share base data efficiently.
    /// </summary>
    private readonly struct ReaderReferenceMap<T> : IDisposable
        where T : class
    {
        private readonly SegmentedList<T> _values;

        private static readonly ObjectPool<SegmentedList<T>> s_objectListPool
            = new(() => new SegmentedList<T>(20));

        private ReaderReferenceMap(SegmentedList<T> values)
            => _values = values;

        public static ReaderReferenceMap<T> Create()
            => new(s_objectListPool.Allocate());

        public void Dispose()
        {
            _values.Clear();
            s_objectListPool.Free(_values);
        }

        public int GetNextObjectId()
        {
            var id = _values.Count;
            _values.Add(null);
            return id;
        }

        public void AddValue(T value)
            => _values.Add(value);

        public void AddValue(int index, T value)
            => _values[index] = value;

        public T GetValue(int referenceId)
            => _values[referenceId];
    }

    internal uint ReadCompressedUInt()
    {
        var info = _reader.ReadByte();
        var marker = (byte)(info & ObjectWriter.ByteMarkerMask);
        var byte0 = (byte)(info & ~ObjectWriter.ByteMarkerMask);

        if (marker == ObjectWriter.Byte1Marker)
        {
            return byte0;
        }

        if (marker == ObjectWriter.Byte2Marker)
        {
            var byte1 = _reader.ReadByte();
            return (((uint)byte0) << 8) | byte1;
        }

        if (marker == ObjectWriter.Byte4Marker)
        {
            var byte1 = _reader.ReadByte();
            var byte2 = _reader.ReadByte();
            var byte3 = _reader.ReadByte();

            return (((uint)byte0) << 24) | (((uint)byte1) << 16) | (((uint)byte2) << 8) | byte3;
        }

        throw ExceptionUtilities.UnexpectedValue(marker);
    }

    private string ReadStringValue()
    {
        var kind = (TypeCode)_reader.ReadByte();
        return kind == TypeCode.Null ? null : ReadStringValue(kind);
    }

    private string ReadStringValue(TypeCode kind)
    {
        return kind switch
        {
            TypeCode.StringRef_1Byte => _stringReferenceMap.GetValue(_reader.ReadByte()),
            TypeCode.StringRef_2Bytes => _stringReferenceMap.GetValue(_reader.ReadUInt16()),
            TypeCode.StringRef_4Bytes => _stringReferenceMap.GetValue(_reader.ReadInt32()),
            TypeCode.StringUtf16 or TypeCode.StringUtf8 => ReadStringLiteral(kind),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    private unsafe string ReadStringLiteral(TypeCode kind)
    {
        string value;
        if (kind == TypeCode.StringUtf8)
        {
            value = _reader.ReadString();
        }
        else
        {
            // This is rare, just allocate UTF-16 bytes for simplicity.
            var characterCount = (int)ReadCompressedUInt();
            var bytes = _reader.ReadBytes(characterCount * sizeof(char));
            fixed (byte* bytesPtr = bytes)
            {
                value = new string((char*)bytesPtr, 0, characterCount);
            }
        }

        _stringReferenceMap.AddValue(value);
        return value;
    }

    private Array ReadArray(TypeCode kind)
    {
        var (length, elementKind) = ReadArrayLengthAndElementKind(kind);

        var elementType = ObjectWriter.s_reverseTypeMap[(int)elementKind];
        if (elementType != null)
        {
            return this.ReadPrimitiveTypeArrayElements(elementType, elementKind, length);
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(elementKind);
        }
    }

    private (int length, TypeCode elementKind) ReadArrayLengthAndElementKind(TypeCode kind)
    {
        var length = kind switch
        {
            TypeCode.Array_0 => 0,
            TypeCode.Array_1 => 1,
            TypeCode.Array_2 => 2,
            TypeCode.Array_3 => 3,
            _ => (int)this.ReadCompressedUInt(),
        };

        // SUBTLE: If it was a primitive array, only the EncodingKind byte of the element type was written, instead of encoding as a type.
        var elementKind = (TypeCode)_reader.ReadByte();

        return (length, elementKind);
    }

    private Array ReadPrimitiveTypeArrayElements(Type type, TypeCode kind, int length)
    {
        Debug.Assert(ObjectWriter.s_reverseTypeMap[(int)kind] == type);

        // optimizations for supported array type by binary reader
        if (type == typeof(byte))
            return _reader.ReadBytes(length);
        if (type == typeof(char))
            return _reader.ReadChars(length);

        // optimizations for string where object reader/writer has its own mechanism to
        // reduce duplicated strings
        if (type == typeof(string))
            return ReadStringArrayElements(CreateArray<string>(length));
        if (type == typeof(bool))
            return ReadBooleanArrayElements(CreateArray<bool>(length));

        // otherwise, read elements directly from underlying binary writer
        return kind switch
        {
            TypeCode.Int8 => ReadInt8ArrayElements(CreateArray<sbyte>(length)),
            TypeCode.Int16 => ReadInt16ArrayElements(CreateArray<short>(length)),
            TypeCode.Int32 => ReadInt32ArrayElements(CreateArray<int>(length)),
            TypeCode.Int64 => ReadInt64ArrayElements(CreateArray<long>(length)),
            TypeCode.UInt16 => ReadUInt16ArrayElements(CreateArray<ushort>(length)),
            TypeCode.UInt32 => ReadUInt32ArrayElements(CreateArray<uint>(length)),
            TypeCode.UInt64 => ReadUInt64ArrayElements(CreateArray<ulong>(length)),
            TypeCode.Float4 => ReadFloat4ArrayElements(CreateArray<float>(length)),
            TypeCode.Float8 => ReadFloat8ArrayElements(CreateArray<double>(length)),
            TypeCode.Decimal => ReadDecimalArrayElements(CreateArray<decimal>(length)),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    private bool[] ReadBooleanArrayElements(bool[] array)
    {
        // Confirm the type to be read below is ulong
        Debug.Assert(BitVector.BitsPerWord == 64);

        var wordLength = BitVector.WordsRequired(array.Length);

        var count = 0;
        for (var i = 0; i < wordLength; i++)
        {
            var word = _reader.ReadUInt64();

            for (var p = 0; p < BitVector.BitsPerWord; p++)
            {
                if (count >= array.Length)
                {
                    return array;
                }

                array[count++] = BitVector.IsTrue(word, p);
            }
        }

        return array;
    }

    private static T[] CreateArray<T>(int length)
    {
        if (length == 0)
        {
            // quick check
            return Array.Empty<T>();
        }
        else
        {
            return new T[length];
        }
    }

    private string[] ReadStringArrayElements(string[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = this.ReadStringValue();
        }

        return array;
    }

    private sbyte[] ReadInt8ArrayElements(sbyte[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadSByte();
        }

        return array;
    }

    private short[] ReadInt16ArrayElements(short[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadInt16();
        }

        return array;
    }

    private int[] ReadInt32ArrayElements(int[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadInt32();
        }

        return array;
    }

    private long[] ReadInt64ArrayElements(long[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadInt64();
        }

        return array;
    }

    private ushort[] ReadUInt16ArrayElements(ushort[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadUInt16();
        }

        return array;
    }

    private uint[] ReadUInt32ArrayElements(uint[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadUInt32();
        }

        return array;
    }

    private ulong[] ReadUInt64ArrayElements(ulong[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadUInt64();
        }

        return array;
    }

    private decimal[] ReadDecimalArrayElements(decimal[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadDecimal();
        }

        return array;
    }

    private float[] ReadFloat4ArrayElements(float[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadSingle();
        }

        return array;
    }

    private double[] ReadFloat8ArrayElements(double[] array)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = _reader.ReadDouble();
        }

        return array;
    }

    public Type ReadType()
    {
        _reader.ReadByte();
        return Type.GetType(ReadString());
    }

    private static Exception DeserializationReadIncorrectNumberOfValuesException(string typeName)
    {
        throw new InvalidOperationException(String.Format(Resources.Deserialization_reader_for_0_read_incorrect_number_of_values, typeName));
    }

    private static Exception NoSerializationTypeException(string typeName)
    {
        return new InvalidOperationException(string.Format(Resources.The_type_0_is_not_understood_by_the_serialization_binder, typeName));
    }

    private static Exception NoSerializationReaderException(string typeName)
    {
        return new InvalidOperationException(string.Format(Resources.Cannot_serialize_type_0, typeName));
    }
}
