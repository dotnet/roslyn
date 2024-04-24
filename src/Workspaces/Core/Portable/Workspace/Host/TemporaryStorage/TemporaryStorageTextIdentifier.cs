//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System.Collections.Immutable;
//using System.Runtime.InteropServices;
//using System.Text;
//using Microsoft.CodeAnalysis.Text;
//using Roslyn.Utilities;

//namespace Microsoft.CodeAnalysis.Host;

///// <summary>
///// Identifier for a <see cref="SourceText"/> placed in a segment of temporary storage (generally a memory mapped file).
///// Can be used to identify that segment across processes, allowing for efficient sharing of data.
///// </summary>
//internal sealed record TemporaryStorageTextIdentifier(
//    string Name,
//    long Offset,
//    long Size,
//    SourceHashAlgorithm ChecksumAlgorithm,
//    Encoding? Encoding,
//    ImmutableArray<byte> ContentHash)
//{
//    public static TemporaryStorageTextIdentifier ReadFrom(ObjectReader reader)
//        => new(
//            reader.ReadRequiredString(),
//            reader.ReadInt64(),
//            reader.ReadInt64(),
//            (SourceHashAlgorithm)reader.ReadInt32(),
//            reader.ReadEncoding(),
//            ImmutableCollectionsMarshal.AsImmutableArray(reader.ReadByteArray()));

//    public void WriteTo(ObjectWriter writer)
//    {
//        writer.WriteString(Name);
//        writer.WriteInt64(Offset);
//        writer.WriteInt64(Size);
//        writer.WriteInt32((int)ChecksumAlgorithm);
//        writer.WriteEncoding(Encoding);
//        writer.WriteByteArray(ImmutableCollectionsMarshal.AsArray(ContentHash)!);
//    }
//}
