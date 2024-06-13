// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal static class MetadataReaderUtils
    {
        internal static IEnumerable<ConstantHandle> GetConstants(this MetadataReader reader)
        {
            for (int i = 1, n = reader.GetTableRowCount(TableIndex.Constant); i <= n; i++)
            {
                yield return MetadataTokens.ConstantHandle(i);
            }
        }

        internal static IEnumerable<ParameterHandle> GetParameters(this MetadataReader reader)
        {
            for (int i = 1, n = reader.GetTableRowCount(TableIndex.Param); i <= n; i++)
            {
                yield return MetadataTokens.ParameterHandle(i);
            }
        }

        internal static IEnumerable<GenericParameterHandle> GetGenericParameters(this MetadataReader reader)
        {
            for (int i = 1, n = reader.GetTableRowCount(TableIndex.GenericParam); i <= n; i++)
            {
                yield return MetadataTokens.GenericParameterHandle(i);
            }
        }

        internal static IEnumerable<GenericParameterConstraintHandle> GetGenericParameterConstraints(this MetadataReader reader)
        {
            for (int i = 1, n = reader.GetTableRowCount(TableIndex.GenericParamConstraint); i <= n; i++)
            {
                yield return MetadataTokens.GenericParameterConstraintHandle(i);
            }
        }

        internal static IEnumerable<ModuleReferenceHandle> GetModuleReferences(this MetadataReader reader)
        {
            for (int i = 1, n = reader.GetTableRowCount(TableIndex.ModuleRef); i <= n; i++)
            {
                yield return MetadataTokens.ModuleReferenceHandle(i);
            }
        }

        internal static IEnumerable<MethodDefinition> GetImportedMethods(this MetadataReader reader)
        {
            return from handle in reader.MethodDefinitions
                   let method = reader.GetMethodDefinition(handle)
                   let import = method.GetImport()
                   where !import.Name.IsNil
                   select method;
        }

        internal static bool RequiresAmdInstructionSet(this PEHeaders headers)
        {
            return headers.CoffHeader.Machine == Machine.Amd64;
        }

        internal static bool Requires64Bits(this PEHeaders headers)
        {
            return headers.PEHeader != null && headers.PEHeader.Magic == PEMagic.PE32Plus
                || headers.CoffHeader.Machine == Machine.Amd64
                || headers.CoffHeader.Machine == Machine.IA64;
        }

        public static string GetString(this IEnumerable<MetadataReader> readers, StringHandle handle)
        {
            int index = MetadataTokens.GetHeapOffset(handle);
            foreach (var reader in readers)
            {
                int length = reader.GetHeapSize(HeapIndex.String);
                if (index < length)
                {
                    return reader.GetString(MetadataTokens.StringHandle(index));
                }
                index -= length;
            }
            return null;
        }

        public static string[] GetStrings(this IEnumerable<MetadataReader> readers, IEnumerable<StringHandle> handles)
        {
            return handles.Select(handle => readers.GetString(handle)).ToArray();
        }

        public static Guid GetModuleVersionId(this MetadataReader reader)
        {
            return reader.GetGuid(reader.GetModuleDefinition().Mvid);
        }

        public static StringHandle[] GetAssemblyRefNames(this MetadataReader reader)
        {
            return reader.AssemblyReferences.Select(handle => reader.GetAssemblyReference(handle).Name).ToArray();
        }

        public static StringHandle[] GetTypeDefNames(this MetadataReader reader)
        {
            return reader.TypeDefinitions.Select(handle => reader.GetTypeDefinition(handle).Name).ToArray();
        }

        public static (StringHandle Namespace, StringHandle Name)[] GetTypeDefFullNames(this MetadataReader reader)
        {
            return reader.TypeDefinitions.Select(handle =>
            {
                var td = reader.GetTypeDefinition(handle);
                return (td.Namespace, td.Name);
            }).ToArray();
        }

        public static StringHandle[] GetTypeRefNames(this MetadataReader reader)
        {
            return reader.TypeReferences.Select(handle => reader.GetTypeReference(handle).Name).ToArray();
        }

        public static StringHandle[] GetEventDefNames(this MetadataReader reader)
        {
            return reader.EventDefinitions.Select(handle => reader.GetEventDefinition(handle).Name).ToArray();
        }

        public static StringHandle[] GetFieldDefNames(this MetadataReader reader)
        {
            return reader.FieldDefinitions.Select(handle => reader.GetFieldDefinition(handle).Name).ToArray();
        }

        public static StringHandle[] GetMethodDefNames(this MetadataReader reader)
        {
            return reader.MethodDefinitions.Select(handle => reader.GetMethodDefinition(handle).Name).ToArray();
        }

        public static StringHandle[] GetMemberRefNames(this MetadataReader reader)
        {
            return reader.MemberReferences.Select(handle => reader.GetMemberReference(handle).Name).ToArray();
        }

        public static StringHandle[] GetParameterDefNames(this MetadataReader reader)
        {
            return reader.GetParameters().Select(handle => reader.GetParameter(handle).Name).ToArray();
        }

        public static StringHandle[] GetPropertyDefNames(this MetadataReader reader)
        {
            return reader.PropertyDefinitions.Select(handle => reader.GetPropertyDefinition(handle).Name).ToArray();
        }

        public static StringHandle GetName(this MetadataReader reader, EntityHandle token)
        {
            switch (token.Kind)
            {
                case HandleKind.TypeReference:
                    return reader.GetTypeReference((TypeReferenceHandle)token).Name;
                case HandleKind.TypeDefinition:
                    return reader.GetTypeDefinition((TypeDefinitionHandle)token).Name;
                default:
                    throw ExceptionUtilities.UnexpectedValue(token.Kind);
            }
        }

        private delegate T ReadBlobItemDelegate<T>(ref BlobReader blobReader);

        private static ImmutableArray<T> ReadArray<T>(this MetadataReader reader, BlobHandle blobHandle, ReadBlobItemDelegate<T> readItem)
        {
            var blobReader = reader.GetBlobReader(blobHandle);
            // Prolog
            blobReader.ReadUInt16();
            // Array size
            int n = blobReader.ReadInt32();
            var builder = ArrayBuilder<T>.GetInstance(n);
            for (int i = 0; i < n; i++)
            {
                builder.Add(readItem(ref blobReader));
            }
            return builder.ToImmutableAndFree();
        }

        public static ImmutableArray<byte> ReadByteArray(this MetadataReader reader, BlobHandle blobHandle)
        {
            return ReadArray(reader, blobHandle, (ref BlobReader blobReader) => blobReader.ReadByte());
        }

        public static ImmutableArray<bool> ReadBoolArray(this MetadataReader reader, BlobHandle blobHandle)
        {
            return ReadArray(reader, blobHandle, (ref BlobReader blobReader) => blobReader.ReadBoolean());
        }

        public static IEnumerable<CustomAttributeRow> GetCustomAttributeRows(this MetadataReader reader)
        {
            foreach (var handle in reader.CustomAttributes)
            {
                var attribute = reader.GetCustomAttribute(handle);
                yield return new CustomAttributeRow(attribute.Parent, attribute.Constructor);
            }
        }

        public static string GetCustomAttributeName(this MetadataReader reader, CustomAttributeRow row)
        {
            EntityHandle parent;
            var token = row.ConstructorToken;
            switch (token.Kind)
            {
                case HandleKind.MemberReference:
                    parent = reader.GetMemberReference((MemberReferenceHandle)token).Parent;
                    break;
                case HandleKind.MethodDefinition:
                    parent = reader.GetMethodDefinition((MethodDefinitionHandle)token).GetDeclaringType();
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(token.Kind);
            }
            var strHandle = reader.GetName(parent);
            return reader.GetString(strHandle);
        }

        public static bool IsIncluded(this ImmutableArray<byte> metadata, string str)
        {
            var builder = ArrayBuilder<byte>.GetInstance();
            builder.AddRange(System.Text.Encoding.UTF8.GetBytes(str));
            builder.Add(0); // Add null terminator.
            var bytes = builder.ToImmutableAndFree();

            for (int i = 0; i < metadata.Length - bytes.Length; i++)
            {
                if (metadata.IsAtIndex(bytes, i))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsAtIndex(this ImmutableArray<byte> metadata, ImmutableArray<byte> bytes, int offset)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                if (metadata[i + offset] != bytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static ImmutableArray<byte> GetSourceLinkBlob(this MetadataReader reader)
        {
            return (from handle in reader.CustomDebugInformation
                    let cdi = reader.GetCustomDebugInformation(handle)
                    where reader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.SourceLink
                    select reader.GetBlobContent(cdi.Value)).Single();
        }

        public static SourceText GetEmbeddedSource(this MetadataReader reader, DocumentHandle document)
        {
            byte[] bytes = (from handle in reader.GetCustomDebugInformation(document)
                            let cdi = reader.GetCustomDebugInformation(handle)
                            where reader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.EmbeddedSource
                            select reader.GetBlobBytes(cdi.Value)).SingleOrDefault();

            if (bytes == null)
            {
                return null;
            }

            int uncompressedSize = BitConverter.ToInt32(bytes, 0);
            var stream = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int));

            if (uncompressedSize != 0)
            {
                var decompressed = new MemoryStream(uncompressedSize);

                using (var deflater = new DeflateStream(stream, CompressionMode.Decompress))
                {
                    deflater.CopyTo(decompressed);
                }

                if (decompressed.Length != uncompressedSize)
                {
                    throw new InvalidDataException();
                }

                stream = decompressed;
            }

            using (stream)
            {
                return EncodedStringText.Create(stream);
            }
        }

        public static IEnumerable<string> DumpAssemblyReferences(this MetadataReader reader)
        {
            return reader.AssemblyReferences.Select(r => reader.GetAssemblyReference(r))
                .Select(row => $"{reader.GetString(row.Name)} {row.Version.Major}.{row.Version.Minor}");
        }

        public static IEnumerable<string> DumpTypeReferences(this MetadataReader reader)
        {
            return reader.TypeReferences
                .Select(t => reader.GetTypeReference(t))
                .Select(t => $"{reader.GetString(t.Name)}, {reader.GetString(t.Namespace)}, {reader.Dump(t.ResolutionScope)}");
        }

        public static string Dump(this MetadataReader reader, EntityHandle handle)
        {
            string value = DumpRec(reader, handle);
            string kind = handle.Kind.ToString();
            if (value != null)
            {
                return $"{kind}:{value}";
            }
            else
            {
                return kind;
            }
        }

        private static string DumpRec(this MetadataReader reader, EntityHandle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.AssemblyReference:
                    return reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)handle).Name);
                case HandleKind.TypeDefinition:
                    {
                        TypeDefinition type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                        return getQualifiedName(type.Namespace, type.Name);
                    }
                case HandleKind.MethodDefinition:
                    {
                        MethodDefinition method = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
                        var blob = reader.GetBlobReader(method.Signature);
                        var decoder = new SignatureDecoder<string, object>(ConstantSignatureVisualizer.Instance, reader, genericContext: null);
                        var signature = decoder.DecodeMethodSignature(ref blob);
                        var parameters = signature.ParameterTypes.Join(", ");
                        return $"{signature.ReturnType} {DumpRec(reader, method.GetDeclaringType())}.{reader.GetString(method.Name)}({parameters})";
                    }
                case HandleKind.MemberReference:
                    {
                        MemberReference member = reader.GetMemberReference((MemberReferenceHandle)handle);
                        var blob = reader.GetBlobReader(member.Signature);
                        var decoder = new SignatureDecoder<string, object>(ConstantSignatureVisualizer.Instance, reader, genericContext: null);
                        var signature = decoder.DecodeMethodSignature(ref blob);
                        var parameters = signature.ParameterTypes.Join(", ");
                        return $"{signature.ReturnType} {DumpRec(reader, member.Parent)}.{reader.GetString(member.Name)}({parameters})";
                    }
                case HandleKind.TypeReference:
                    {
                        TypeReference type = reader.GetTypeReference((TypeReferenceHandle)handle);
                        return getQualifiedName(type.Namespace, type.Name);
                    }
                case HandleKind.FieldDefinition:
                    {
                        FieldDefinition field = reader.GetFieldDefinition((FieldDefinitionHandle)handle);
                        var name = reader.GetString(field.Name);

                        var blob = reader.GetBlobReader(field.Signature);
                        var decoder = new SignatureDecoder<string, object>(ConstantSignatureVisualizer.Instance, reader, genericContext: null);
                        var type = decoder.DecodeFieldSignature(ref blob);

                        return $"{type} {name}";
                    }
                case HandleKind.TypeSpecification:
                    {
                        var typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)handle);
                        var blob = reader.GetBlobReader(typeSpec.Signature);
                        var decoder = new SignatureDecoder<string, object>(ConstantSignatureVisualizer.Instance, reader, genericContext: null);
                        var type = decoder.DecodeType(ref blob);

                        return $"{type}";
                    }
                default:
                    return null;
            }

            string getQualifiedName(StringHandle leftHandle, StringHandle rightHandle)
            {
                string name = reader.GetString(rightHandle);
                if (!leftHandle.IsNil)
                {
                    name = reader.GetString(leftHandle) + "." + name;
                }
                return name;
            }
        }

        private sealed class ConstantSignatureVisualizer : ISignatureTypeProvider<string, object>
        {
            public static readonly ConstantSignatureVisualizer Instance = new ConstantSignatureVisualizer();

            public string GetArrayType(string elementType, ArrayShape shape)
                => elementType + "[" + new string(',', shape.Rank) + "]";

            public string GetByReferenceType(string elementType)
                => elementType + "&";

            public string GetFunctionPointerType(MethodSignature<string> signature)
                => "method-ptr";

            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
                => genericType + "{" + string.Join(", ", typeArguments) + "}";

            public string GetGenericMethodParameter(object genericContext, int index)
                => "!!" + index;

            public string GetGenericTypeParameter(object genericContext, int index)
                => "!" + index;

            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
                => (isRequired ? "modreq" : "modopt") + "(" + modifier + ") " + unmodifiedType;

            public string GetPinnedType(string elementType)
                => "pinned " + elementType;

            public string GetPointerType(string elementType)
                => elementType + "*";

            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
                => typeCode.ToString();

            public string GetSZArrayType(string elementType)
                => elementType + "[]";

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var typeDef = reader.GetTypeDefinition(handle);
                var name = reader.GetString(typeDef.Name);
                return typeDef.Namespace.IsNil ? name : reader.GetString(typeDef.Namespace) + "." + name;
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var typeRef = reader.GetTypeReference(handle);
                var name = reader.GetString(typeRef.Name);
                return typeRef.Namespace.IsNil ? name : reader.GetString(typeRef.Namespace) + "." + name;
            }

            public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<string, object>(Instance, reader, genericContext).DecodeType(ref sigReader);
            }
        }

        internal static void VerifyPEMetadata(string pePath, string[] types, string[] methods, string[] attributes)
        {
            using (var peStream = File.OpenRead(pePath))
            using (var refPeReader = new PEReader(peStream))
            {
                var metadataReader = refPeReader.GetMetadataReader();

                AssertEx.SetEqual(metadataReader.TypeDefinitions.Select(t => metadataReader.Dump(t)), types);
                AssertEx.SetEqual(metadataReader.MethodDefinitions.Select(t => metadataReader.Dump(t)), methods);

                AssertEx.SetEqual(
                    metadataReader.CustomAttributes.Select(a => metadataReader.GetCustomAttribute(a).Constructor)
                        .Select(c => getAttributeTypeName(metadataReader, c))
                        .Select(n => metadataReader.GetString(n)),
                    attributes);
            }

            static StringHandle getAttributeTypeName(MetadataReader metadataReader, EntityHandle constructorHandle)
            {
                // See MetadataWriter.GetCustomAttributeTypeCodedIndex
                if (constructorHandle.Kind == HandleKind.MemberReference)
                {
                    var typeRef = metadataReader.GetMemberReference((MemberReferenceHandle)constructorHandle).Parent;
                    return metadataReader.GetTypeReference((TypeReferenceHandle)typeRef).Name;
                }
                else
                {
                    Debug.Assert(constructorHandle.Kind == HandleKind.MethodDefinition);
                    var typeDef = metadataReader.GetMethodDefinition((MethodDefinitionHandle)constructorHandle).GetDeclaringType();
                    return metadataReader.GetTypeDefinition(typeDef).Name;
                }
            }
        }

        internal static void VerifyMethodBodies(ImmutableArray<byte> peImage, Action<byte[]> ilValidator)
        {
            using (var peReader = new PEReader(peImage))
            {
                var metadataReader = peReader.GetMetadataReader();
                foreach (var method in metadataReader.MethodDefinitions)
                {
                    var rva = metadataReader.GetMethodDefinition(method).RelativeVirtualAddress;
                    if (rva != 0)
                    {
                        var il = peReader.GetMethodBody(rva).GetILBytes();
                        ilValidator(il);
                    }
                    else
                    {
                        ilValidator(null);
                    }
                }
            }
        }

        static readonly byte[] ThrowNull = new[] { (byte)ILOpCode.Ldnull, (byte)ILOpCode.Throw };

        internal static void AssertEmptyOrThrowNull(ImmutableArray<byte> peImage)
        {
            VerifyMethodBodies(peImage, (il) =>
            {
                if (il != null)
                {
                    AssertEx.Equal(ThrowNull, il);
                }
            });
        }

        internal static void AssertNotThrowNull(ImmutableArray<byte> peImage)
        {
            VerifyMethodBodies(peImage, (il) =>
            {
                if (il != null)
                {
                    AssertEx.NotEqual(ThrowNull, il);
                }
            });
        }
    }
}
