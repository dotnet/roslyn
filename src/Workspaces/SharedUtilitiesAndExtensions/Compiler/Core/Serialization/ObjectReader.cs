// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SQLite.Interop;

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

    private readonly bool _leaveOpen;
    private readonly PipeReader _reader;

    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Map of reference id's to deserialized strings.
    /// </summary>
    private readonly ReaderReferenceMap<string> _stringReferenceMap;

    /// <summary>
    /// Creates a new instance of a <see cref="ObjectReader"/>.
    /// </summary>
    /// <param name="reader">The stream to read objects from.</param>
    /// <param name="leaveOpen">True to leave the <paramref name="reader"/> open after the <see cref="ObjectWriter"/> is disposed.</param>
    /// <param name="cancellationToken"></param>
    private ObjectReader(
        PipeReader reader,
        bool leaveOpen,
        CancellationToken cancellationToken)
    {
        // String serialization assumes both reader and writer to be of the same endianness.
        // It can be adjusted for BigEndian if needed.
        Debug.Assert(BitConverter.IsLittleEndian);

        _reader = reader;
        _leaveOpen = leaveOpen;
        _stringReferenceMap = ReaderReferenceMap<string>.Create();

        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Attempts to create a <see cref="ObjectReader"/> from the provided <paramref name="reader"/>. If the <paramref
    /// name="reader"/> does not start with a valid header, then <see langword="null"/> will be returned.
    /// </summary>
    public static async ValueTask<ObjectReader> TryGetReaderAsync(
        PipeReader reader,
        bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        const int preambleSize = 2;
        if (reader == null)
            return null;

        try
        {
            var readResult = await reader.ReadAtLeastAsync(preambleSize, cancellationToken).ConfigureAwait(false);
            return CreateReaderFromResult(readResult);
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

            return null;
        }

        ObjectReader CreateReaderFromResult(ReadResult result)
        {
            Span<byte> preamble = stackalloc byte[preambleSize];
            result.Buffer.Slice(0, preambleSize).CopyTo(preamble);

            if (preamble[0] != VersionByte1 ||
                preamble[1] != VersionByte2)
            {
                return null;
            }

            reader.AdvanceTo(result.Buffer.GetPosition(preambleSize));
            return new ObjectReader(reader, leaveOpen, cancellationToken);
        }
    }

    /// <summary>
    /// Creates an <see cref="ObjectReader"/> from the provided <paramref name="reader"/>. Unlike <see
    /// cref="TryGetReaderAsync"/>, it requires the version of the data in the stream to exactly match the current
    /// format version. Should only be used to read data written by the same version of Roslyn.
    /// </summary>
    public static async ValueTask<ObjectReader> GetReaderAsync(
        PipeReader reader,
        bool leaveOpen,
        CancellationToken cancellationToken)
    {
        var objectReader = new ObjectReader(reader, leaveOpen, cancellationToken);

        var b = await objectReader.ReadByteAsync().ConfigureAwait(false);
        if (b != VersionByte1)
            throw ExceptionUtilities.UnexpectedValue(b);

        b = await objectReader.ReadByteAsync().ConfigureAwait(false);
        if (b != VersionByte2)
            throw ExceptionUtilities.UnexpectedValue(b);

        return objectReader;
    }

    public void Dispose()
    {
        if (!_leaveOpen)
            _reader.Complete();

        _stringReferenceMap.Dispose();
    }

    public async ValueTask<bool> ReadBooleanAsync()
        => await ReadByteAsync().ConfigureAwait(false) != 0;

    public async ValueTask<byte> ReadByteAsync()
    {
        const int byteCount = 1;
        var readResult = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);
        var result = readResult.Buffer.FirstSpan[0];
        _reader.AdvanceTo(readResult.Buffer.GetPosition(byteCount));
        return result;
    }

    public async ValueTask<sbyte> ReadSByteAsync()
        => unchecked((sbyte)await ReadByteAsync().ConfigureAwait(false));

    public async ValueTask<short> ReadInt16Async()
    {
        const int byteCount = 2;
        var readResult = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);
        var result = ReadValue(readResult);
        _reader.AdvanceTo(readResult.Buffer.GetPosition(byteCount));
        return result;

        static short ReadValue(ReadResult result)
        {
            Span<byte> dest = stackalloc byte[byteCount];
            result.Buffer.Slice(0, byteCount).CopyTo(dest);
            return BinaryPrimitives.ReadInt16LittleEndian(dest);
        }
    }

    public async ValueTask<ushort> ReadUInt16Async()
        => unchecked((ushort)await ReadInt16Async().ConfigureAwait(false));

    public async ValueTask<int> ReadInt32Async()
    {
        const int byteCount = 4;
        var readResult = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);
        var result = ReadValue(readResult);
        _reader.AdvanceTo(readResult.Buffer.GetPosition(byteCount));
        return result;

        static int ReadValue(ReadResult result)
        {
            Span<byte> dest = stackalloc byte[byteCount];
            result.Buffer.Slice(0, byteCount).CopyTo(dest);
            return BinaryPrimitives.ReadInt32LittleEndian(dest);
        }
    }

    public async ValueTask<uint> ReadUInt32Async()
        => unchecked((uint)await ReadInt32Async().ConfigureAwait(false));

    public async ValueTask<long> ReadInt64Async()
    {
        const int byteCount = 8;
        var readResult = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);
        var result = ReadValue(readResult);
        _reader.AdvanceTo(readResult.Buffer.GetPosition(byteCount));
        return result;

        static long ReadValue(ReadResult result)
        {
            Span<byte> dest = stackalloc byte[byteCount];
            result.Buffer.Slice(0, byteCount).CopyTo(dest);
            return BinaryPrimitives.ReadInt64LittleEndian(dest);
        }
    }

    public async ValueTask<ulong> ReadUInt64Async()
        => unchecked((ulong)await ReadInt64Async().ConfigureAwait(false));

    public async ValueTask<double> ReadDoubleAsync()
        => BitConverter.Int64BitsToDouble(await ReadInt64Async().ConfigureAwait(false));

    public async ValueTask<float> ReadSingleAsync()
        => BitConverter.Int32BitsToSingle(await ReadInt32Async().ConfigureAwait(false));

    // read as ushort because BinaryWriter fails on chars that are unicode surrogates
    public async ValueTask<char> ReadCharAsync()
        => (char)await ReadUInt16Async().ConfigureAwait(false);

    public async ValueTask<decimal> ReadDecimalAsync()
    {
        return new decimal(
            isNegative: await ReadBooleanAsync().ConfigureAwait(false),
            scale: await ReadByteAsync().ConfigureAwait(false),
            lo: await ReadInt32Async().ConfigureAwait(false),
            mid: await ReadInt32Async().ConfigureAwait(false),
            hi: await ReadInt32Async().ConfigureAwait(false));
    }

    public async ValueTask<Guid> ReadGuidAsync()
    {
        var accessor = new ObjectWriter.GuidAccessor
        {
            Low64 = await ReadInt64Async().ConfigureAwait(false),
            High64 = await ReadInt64Async().ConfigureAwait(false),
        };

        return accessor.Guid;
    }

    public async ValueTask<object> ReadValueAsync()
    {
        var code = (TypeCode)await ReadByteAsync().ConfigureAwait(false);
        switch (code)
        {
            case TypeCode.Null: return null;
            case TypeCode.Boolean_True: return true;
            case TypeCode.Boolean_False: return false;
            case TypeCode.Int8: return await ReadSByteAsync().ConfigureAwait(false);
            case TypeCode.UInt8: return await ReadByteAsync().ConfigureAwait(false);
            case TypeCode.Int16: return await ReadInt16Async().ConfigureAwait(false);
            case TypeCode.UInt16: return await ReadUInt16Async().ConfigureAwait(false);
            case TypeCode.Int32: return await ReadInt32Async().ConfigureAwait(false);
            case TypeCode.Int32_1Byte: return (int)await ReadByteAsync().ConfigureAwait(false);
            case TypeCode.Int32_2Bytes: return (int)await ReadUInt16Async().ConfigureAwait(false);
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
            case TypeCode.UInt32: return await ReadUInt32Async().ConfigureAwait(false);
            case TypeCode.UInt32_1Byte: return (uint)await ReadByteAsync().ConfigureAwait(false);
            case TypeCode.UInt32_2Bytes: return (uint)await ReadUInt16Async().ConfigureAwait(false);
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
            case TypeCode.Int64: return await ReadInt64Async().ConfigureAwait(false);
            case TypeCode.UInt64: return await ReadUInt64Async().ConfigureAwait(false);
            case TypeCode.Float4: return await ReadSingleAsync().ConfigureAwait(false);
            case TypeCode.Float8: return await ReadDoubleAsync().ConfigureAwait(false);
            case TypeCode.Decimal: return await ReadDecimalAsync().ConfigureAwait(false);
            case TypeCode.Char:
                // read as ushort because BinaryWriter fails on chars that are unicode surrogates
                return (char)await ReadUInt16Async().ConfigureAwait(false);
            case TypeCode.StringUtf8:
            case TypeCode.StringUtf16:
            case TypeCode.StringRef_4Bytes:
            case TypeCode.StringRef_1Byte:
            case TypeCode.StringRef_2Bytes:
                return await ReadStringAsync(code).ConfigureAwait(false);
            case TypeCode.DateTime:
                return DateTime.FromBinary(await ReadInt64Async().ConfigureAwait(false));
            case TypeCode.Array:
            case TypeCode.Array_0:
            case TypeCode.Array_1:
            case TypeCode.Array_2:
            case TypeCode.Array_3:
                return await ReadArrayAsync(code).ConfigureAwait(false);

            case TypeCode.EncodingName:
                return Encoding.GetEncoding(await ReadStringAsync().ConfigureAwait(false));

            case >= TypeCode.FirstWellKnownTextEncoding and <= TypeCode.LastWellKnownTextEncoding:
                return ObjectWriter.ToEncodingKind(code).GetEncoding();

            case TypeCode.EncodingCodePage:
                return Encoding.GetEncoding(await ReadInt32Async().ConfigureAwait(false));

            default:
                throw ExceptionUtilities.UnexpectedValue(code);
        }
    }

    public async ValueTask<(char[] array, int length)> ReadCharArrayAsync(Func<int, char[]> getArray)
    {
        var kind = (TypeCode)await ReadByteAsync().ConfigureAwait(false);

        (var length, _) = await ReadArrayLengthAndElementKindAsync(kind).ConfigureAwait(false);
        var array = getArray(length);

        var byteCount = length * 2;
        var readResult = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);
        readResult.Buffer.Slice(byteCount).CopyTo(MemoryMarshal.AsBytes(array.AsSpan()));
        return (array, length);
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

    internal async ValueTask<uint> ReadCompressedUIntAsync()
    {
        var info = await ReadByteAsync().ConfigureAwait(false);
        var marker = (byte)(info & ObjectWriter.ByteMarkerMask);
        var byte0 = (byte)(info & ~ObjectWriter.ByteMarkerMask);

        if (marker == ObjectWriter.Byte1Marker)
        {
            return byte0;
        }

        if (marker == ObjectWriter.Byte2Marker)
        {
            var byte1 = await ReadByteAsync().ConfigureAwait(false);
            return (((uint)byte0) << 8) | byte1;
        }

        if (marker == ObjectWriter.Byte4Marker)
        {
            var byte1 = await ReadByteAsync().ConfigureAwait(false);
            var byte2 = await ReadByteAsync().ConfigureAwait(false);
            var byte3 = await ReadByteAsync().ConfigureAwait(false);

            return (((uint)byte0) << 24) | (((uint)byte1) << 16) | (((uint)byte2) << 8) | byte3;
        }

        throw ExceptionUtilities.UnexpectedValue(marker);
    }

    private async ValueTask<string> ReadStringAsync()
    {
        var kind = (TypeCode)await ReadByteAsync().ConfigureAwait(false);
        if (kind == TypeCode.Null)
            return null;

        return await ReadStringAsync(kind).ConfigureAwait(false);
    }

    private async ValueTask<string> ReadStringAsync(TypeCode kind)
    {
        return kind switch
        {
            TypeCode.StringRef_1Byte => _stringReferenceMap.GetValue(await ReadByteAsync().ConfigureAwait(false)),
            TypeCode.StringRef_2Bytes => _stringReferenceMap.GetValue(await ReadUInt16Async().ConfigureAwait(false)),
            TypeCode.StringRef_4Bytes => _stringReferenceMap.GetValue(await ReadInt32Async().ConfigureAwait(false)),
            TypeCode.StringUtf16 or TypeCode.StringUtf8 => await ReadStringContentsAsync(kind).ConfigureAwait(false),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };

        async ValueTask<string> ReadStringContentsAsync(TypeCode kind)
        {
            var value = kind == TypeCode.StringUtf8
                ? await ReadUtf8StringContentsAsync().ConfigureAwait(false)
                : await ReadUtf16StringContentsAsync().ConfigureAwait(false);

            _stringReferenceMap.AddValue(value);
            return value;
        }

        async ValueTask<string> ReadUtf8StringContentsAsync()
        {
            var byteCount = await ReadInt32Async().ConfigureAwait(false);
            var result = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);

#if NETSTANDARD
                var bytes = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount);
                result.Buffer.Slice(0, byteCount).CopyTo(bytes);
                value = Encoding.UTF8.GetString(bytes);
                System.Buffers.ArrayPool<byte>.Shared.Return(bytes);
#else
            var value = Encoding.UTF8.GetString(result.Buffer);
#endif
            _reader.AdvanceTo(result.Buffer.GetPosition(byteCount));

            return value;
        }

        async ValueTask<string> ReadUtf16StringContentsAsync()
        {
            var byteCount = await ReadInt32Async().ConfigureAwait(false);
            Contract.ThrowIfTrue(byteCount % 2 == 1);

            var result = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);
            return ReadUtf16StringContents(byteCount, result);
        }

        string ReadUtf16StringContents(int byteCount, ReadResult result)
        {
            var chars = new char[byteCount / 2];
            var bytes = MemoryMarshal.AsBytes(chars.AsSpan());
            result.Buffer.Slice(byteCount).CopyTo(bytes);

            var value = new string(chars);
            _reader.AdvanceTo(result.Buffer.GetPosition(byteCount));
            return value;
        }
    }

    private async ValueTask<Array> ReadArrayAsync(TypeCode kind)
    {
        var (length, elementKind) = await ReadArrayLengthAndElementKindAsync(kind).ConfigureAwait(false);

        var elementType = ObjectWriter.s_reverseTypeMap[(int)elementKind];
        if (elementType != null)
        {
            return await this.ReadPrimitiveTypeArrayElementsAsync(elementType, elementKind, length).ConfigureAwait(false);
        }
        else
        {
            throw ExceptionUtilities.UnexpectedValue(elementKind);
        }
    }

    private async ValueTask<(int length, TypeCode elementKind)> ReadArrayLengthAndElementKindAsync(TypeCode kind)
    {
        var length = kind switch
        {
            TypeCode.Array_0 => 0,
            TypeCode.Array_1 => 1,
            TypeCode.Array_2 => 2,
            TypeCode.Array_3 => 3,
            _ => (int)await this.ReadCompressedUIntAsync().ConfigureAwait(false),
        };

        // SUBTLE: If it was a primitive array, only the EncodingKind byte of the element type was written, instead of encoding as a type.
        var elementKind = (TypeCode)await ReadByteAsync().ConfigureAwait(false);

        return (length, elementKind);
    }

    private async ValueTask<Array> ReadPrimitiveTypeArrayElementsAsync(Type type, TypeCode kind, int length)
    {
        Debug.Assert(ObjectWriter.s_reverseTypeMap[(int)kind] == type);

        // optimizations for supported array type by binary reader
        if (type == typeof(byte))
        {
            var result = new byte[length];
            var readResult = await _reader.ReadAtLeastAsync(length, _cancellationToken).ConfigureAwait(false);
            readResult.Buffer.Slice(length).CopyTo(result);
            return result;
        }
        if (type == typeof(char))
        {
            var result = new char[length];
            var byteCount = length * 2;
            var readResult = await _reader.ReadAtLeastAsync(byteCount, _cancellationToken).ConfigureAwait(false);
            readResult.Buffer.Slice(byteCount).CopyTo(MemoryMarshal.AsBytes(result.AsSpan()));
            return result;
        }

        // optimizations for string where object reader/writer has its own mechanism to
        // reduce duplicated strings
        if (type == typeof(string))
            return await ReadStringArrayElementsAsync(CreateArray<string>(length)).ConfigureAwait(false);
        if (type == typeof(bool))
            return await ReadBooleanArrayElementsAsync(CreateArray<bool>(length)).ConfigureAwait(false);

        // otherwise, read elements directly from underlying binary writer
        return kind switch
        {
            TypeCode.Int8 => await ReadInt8ArrayElementsAsync(CreateArray<sbyte>(length)).ConfigureAwait(false),
            TypeCode.Int16 => await ReadInt16ArrayElementsAsync(CreateArray<short>(length)).ConfigureAwait(false),
            TypeCode.Int32 => await ReadInt32ArrayElementsAsync(CreateArray<int>(length)).ConfigureAwait(false),
            TypeCode.Int64 => await ReadInt64ArrayElementsAsync(CreateArray<long>(length)).ConfigureAwait(false),
            TypeCode.UInt16 => await ReadUInt16ArrayElementsAsync(CreateArray<ushort>(length)).ConfigureAwait(false),
            TypeCode.UInt32 => await ReadUInt32ArrayElementsAsync(CreateArray<uint>(length)).ConfigureAwait(false),
            TypeCode.UInt64 => await ReadUInt64ArrayElementsAsync(CreateArray<ulong>(length)).ConfigureAwait(false),
            TypeCode.Float4 => await ReadFloat4ArrayElementsAsync(CreateArray<float>(length)).ConfigureAwait(false),
            TypeCode.Float8 => await ReadFloat8ArrayElementsAsync(CreateArray<double>(length)).ConfigureAwait(false),
            TypeCode.Decimal => await ReadDecimalArrayElementsAsync(CreateArray<decimal>(length)).ConfigureAwait(false),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    private async ValueTask<bool[]> ReadBooleanArrayElementsAsync(bool[] array)
    {
        // Confirm the type to be read below is ulong
        Debug.Assert(BitVector.BitsPerWord == 64);

        var wordLength = BitVector.WordsRequired(array.Length);

        var count = 0;
        for (var i = 0; i < wordLength; i++)
        {
            var word = await ReadUInt64Async().ConfigureAwait(false);

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
            return [];
        }
        else
        {
            return new T[length];
        }
    }

    private async ValueTask<string[]> ReadStringArrayElementsAsync(string[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await this.ReadStringAsync().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<sbyte[]> ReadInt8ArrayElementsAsync(sbyte[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadSByteAsync().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<short[]> ReadInt16ArrayElementsAsync(short[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadInt16Async().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<int[]> ReadInt32ArrayElementsAsync(int[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadInt32Async().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<long[]> ReadInt64ArrayElementsAsync(long[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadInt64Async().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<ushort[]> ReadUInt16ArrayElementsAsync(ushort[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadUInt16Async().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<uint[]> ReadUInt32ArrayElementsAsync(uint[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadUInt32Async().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<ulong[]> ReadUInt64ArrayElementsAsync(ulong[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadUInt64Async().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<decimal[]> ReadDecimalArrayElementsAsync(decimal[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadDecimalAsync().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<float[]> ReadFloat4ArrayElementsAsync(float[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadSingleAsync().ConfigureAwait(false);

        return array;
    }

    private async ValueTask<double[]> ReadFloat8ArrayElementsAsync(double[] array)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = await ReadDoubleAsync().ConfigureAwait(false);

        return array;
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
