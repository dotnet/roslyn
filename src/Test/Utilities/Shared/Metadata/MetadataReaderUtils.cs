// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.IO.Compression;

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

        public static string GetString(this MetadataReader[] readers, StringHandle handle)
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

        public static string[] GetStrings(this MetadataReader[] readers, StringHandle[] handles)
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
                    var typeRef = reader.GetTypeReference((TypeReferenceHandle)token);
                    return typeRef.Name;
                default:
                    throw ExceptionUtilities.UnexpectedValue(token.Kind);
            }
        }

        public static IEnumerable<CustomAttributeRow> GetCustomAttributeRows(this MetadataReader reader)
        {
            foreach (var handle in reader.CustomAttributes)
            {
                var attribute = reader.GetCustomAttribute(handle);
                yield return new CustomAttributeRow(attribute.Parent, attribute.Constructor);
            }
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
            switch (handle.Kind)
            {
                case HandleKind.AssemblyReference:
                    return "AssemblyRef:" + reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)handle).Name);
                default:
                    return handle.Kind.ToString();
            }
        }
    }
}
