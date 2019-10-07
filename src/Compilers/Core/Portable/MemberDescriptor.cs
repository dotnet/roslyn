// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.RuntimeMembers
{
    [Flags()]
    internal enum MemberFlags : byte
    {
        // BEGIN Mutually exclusive Member kinds:
        Method = 0x01,
        Field = 0x02,
        Constructor = 0x04,
        PropertyGet = 0x08,
        Property = 0x10,
        // END Mutually exclusive Member kinds

        KindMask = 0x1F,

        Static = 0x20,
        Virtual = 0x40, // Virtual in CLR terms, i.e. sealed should be accepted.
    }

    /// <summary>
    /// Structure that describes a member of a type.
    /// </summary>
    internal struct MemberDescriptor
    {
        public readonly MemberFlags Flags;

        /// <summary>
        /// Id/token of containing type, usually value from some enum.
        /// For example from SpecialType enum.
        /// I am not using SpecialType as the type for this field because
        /// VB runtime types are not part of SpecialType.
        /// 
        /// So, the implication is that any type ids we use outside of the SpecialType 
        /// (either for the VB runtime classes, or types like System.Task etc.) will need 
        /// to use IDs that are all mutually disjoint. 
        /// </summary>
        public readonly short DeclaringTypeId;

        public string? DeclaringTypeMetadataName
        {
            get
            {
                return DeclaringTypeId <= (int)SpecialType.Count
                           ? ((SpecialType)DeclaringTypeId).GetMetadataName()
                           : ((WellKnownType)DeclaringTypeId).GetMetadataName();
            }
        }

        public readonly ushort Arity;
        public readonly string Name;

        /// <summary>
        /// Signature of the field or method, similar to metadata signature, 
        /// but with the following exceptions:
        ///    1) Truncated on the left, for methods starts at [ParamCount], for fields at [Type]
        ///    2) Type tokens are not compressed
        ///    3) BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U | Void types are encoded by 
        ///       using VALUETYPE+typeId notation.
        ///    4) array bounds are not included.
        ///    5) modifiers are not included.
        ///    6) (CLASS | VALUETYPE) are omitted after GENERICINST
        /// </summary>
        public readonly ImmutableArray<byte> Signature;

        /// <summary>
        /// Applicable only to properties and methods, throws otherwise.
        /// </summary>
        public int ParametersCount
        {
            get
            {
                MemberFlags memberKind = Flags & MemberFlags.KindMask;
                switch (memberKind)
                {
                    case MemberFlags.Constructor:
                    case MemberFlags.Method:
                    case MemberFlags.PropertyGet:
                    case MemberFlags.Property:
                        return Signature[0];
                    default:
                        throw ExceptionUtilities.UnexpectedValue(memberKind);
                }
            }
        }

        public MemberDescriptor(
            MemberFlags Flags,
            short DeclaringTypeId,
            string Name,
            ImmutableArray<byte> Signature,
            ushort Arity = 0)
        {
            this.Flags = Flags;
            this.DeclaringTypeId = DeclaringTypeId;
            this.Name = Name;
            this.Arity = Arity;
            this.Signature = Signature;
        }

        internal static ImmutableArray<MemberDescriptor> InitializeFromStream(Stream stream, string[] nameTable)
        {
            int count = nameTable.Length;

            var builder = ImmutableArray.CreateBuilder<MemberDescriptor>(count);
            var signatureBuilder = ImmutableArray.CreateBuilder<byte>();

            for (int i = 0; i < count; i++)
            {
                MemberFlags flags = (MemberFlags)stream.ReadByte();
                short declaringTypeId = ReadTypeId(stream);
                ushort arity = (ushort)stream.ReadByte();

                if ((flags & MemberFlags.Field) != 0)
                {
                    ParseType(signatureBuilder, stream);
                }
                else
                {
                    // Property, PropertyGet, Method or Constructor
                    ParseMethodOrPropertySignature(signatureBuilder, stream);
                }

                builder.Add(new MemberDescriptor(flags, declaringTypeId, nameTable[i], signatureBuilder.ToImmutable(), arity));
                signatureBuilder.Clear();
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// The type Id may be:
        ///     (1) encoded in a single byte (for types below 255)
        ///     (2) encoded in two bytes (255 + extension byte) for types below 512
        /// </summary>
        private static short ReadTypeId(Stream stream)
        {
            var firstByte = (byte)stream.ReadByte();

            if (firstByte == (byte)WellKnownType.ExtSentinel)
            {
                return (short)(stream.ReadByte() + WellKnownType.ExtSentinel);
            }
            else
            {
                return firstByte;
            }
        }

        private static void ParseMethodOrPropertySignature(ImmutableArray<byte>.Builder builder, Stream stream)
        {
            int paramCount = stream.ReadByte();
            builder.Add((byte)paramCount);

            // Return type
            ParseType(builder, stream, allowByRef: true);

            // Parameters
            for (int i = 0; i < paramCount; i++)
            {
                ParseType(builder, stream, allowByRef: true);
            }
        }

        private static void ParseType(ImmutableArray<byte>.Builder builder, Stream stream, bool allowByRef = false)
        {
            while (true)
            {
                var typeCode = (SignatureTypeCode)stream.ReadByte();
                builder.Add((byte)typeCode);

                switch (typeCode)
                {
                    default:
                        throw ExceptionUtilities.UnexpectedValue(typeCode);

                    case SignatureTypeCode.TypeHandle:
                        ParseTypeHandle(builder, stream);
                        return;

                    case SignatureTypeCode.GenericTypeParameter:
                    case SignatureTypeCode.GenericMethodParameter:
                        builder.Add((byte)stream.ReadByte());
                        return;

                    case SignatureTypeCode.ByReference:
                        if (!allowByRef) goto default;
                        break;

                    case SignatureTypeCode.SZArray:
                        break;

                    case SignatureTypeCode.Pointer:
                        break;

                    case SignatureTypeCode.GenericTypeInstance:
                        ParseGenericTypeInstance(builder, stream);
                        return;
                }

                allowByRef = false;
            }
        }

        /// <summary>
        /// Read a type Id from the stream and copy it into the builder.
        /// This may copy one or two bytes depending on the first one.
        /// </summary>
        private static void ParseTypeHandle(ImmutableArray<byte>.Builder builder, Stream stream)
        {
            var firstByte = (byte)stream.ReadByte();
            builder.Add(firstByte);

            if (firstByte == (byte)WellKnownType.ExtSentinel)
            {
                var secondByte = (byte)stream.ReadByte();
                builder.Add(secondByte);
            }
        }

        private static void ParseGenericTypeInstance(ImmutableArray<byte>.Builder builder, Stream stream)
        {
            ParseType(builder, stream);

            // Generic type parameters
            int argumentCount = stream.ReadByte();
            builder.Add((byte)argumentCount);
            for (int i = 0; i < argumentCount; i++)
            {
                ParseType(builder, stream);
            }
        }
    }
}
