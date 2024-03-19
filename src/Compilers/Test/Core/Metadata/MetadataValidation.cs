// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.Metadata.Tools;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.Test.Utilities
{
    public static class MetadataValidation
    {
        /// <summary>
        /// Returns the name of the attribute class 
        /// </summary>
        internal static string GetAttributeName(MetadataReader metadataReader, CustomAttributeHandle customAttribute)
        {
            var ctorHandle = metadataReader.GetCustomAttribute(customAttribute).Constructor;
            if (ctorHandle.Kind == HandleKind.MemberReference) // MemberRef
            {
                var container = metadataReader.GetMemberReference((MemberReferenceHandle)ctorHandle).Parent;
                var name = metadataReader.GetTypeReference((TypeReferenceHandle)container).Name;
                return metadataReader.GetString(name);
            }
            else if (ctorHandle.Kind == HandleKind.MethodDefinition)
            {
                var container = metadataReader.GetMethodDefinition((MethodDefinitionHandle)ctorHandle).GetDeclaringType();
                var name = metadataReader.GetTypeDefinition(container).Name;
                return metadataReader.GetString(name);
            }
            else
            {
                Assert.True(false, "not impl");
                return null;
            }
        }

        internal static CustomAttributeHandle FindCustomAttribute(MetadataReader metadataReader, string attributeClassName)
        {
            foreach (var caHandle in metadataReader.CustomAttributes)
            {
                if (string.Equals(GetAttributeName(metadataReader, caHandle), attributeClassName, StringComparison.Ordinal))
                {
                    return caHandle;
                }
            }

            return default(CustomAttributeHandle);
        }

        /// <summary>
        /// Used to validate metadata blobs emitted for MarshalAs.
        /// </summary>
        internal static void MarshalAsMetadataValidator(PEAssembly assembly, Func<string, PEAssembly, byte[]> getExpectedBlob, bool isField = true)
        {
            var metadataReader = assembly.GetMetadataReader();

            // no custom attributes should be emitted on parameters, fields or methods:
            foreach (var ca in metadataReader.CustomAttributes)
            {
                Assert.NotEqual("MarshalAsAttribute", GetAttributeName(metadataReader, ca));
            }

            int expectedMarshalCount = 0;

            if (isField)
            {
                // fields
                foreach (var fieldDef in metadataReader.FieldDefinitions)
                {
                    var field = metadataReader.GetFieldDefinition(fieldDef);
                    string fieldName = metadataReader.GetString(field.Name);

                    byte[] expectedBlob = getExpectedBlob(fieldName, assembly);
                    if (expectedBlob != null)
                    {
                        BlobHandle descriptor = metadataReader.GetFieldDefinition(fieldDef).GetMarshallingDescriptor();
                        Assert.False(descriptor.IsNil, "Expecting record in FieldMarshal table");

                        Assert.NotEqual(0, (int)(field.Attributes & FieldAttributes.HasFieldMarshal));
                        expectedMarshalCount++;

                        byte[] actualBlob = metadataReader.GetBlobBytes(descriptor);
                        AssertEx.Equal(expectedBlob, actualBlob);
                    }
                    else
                    {
                        Assert.Equal(0, (int)(field.Attributes & FieldAttributes.HasFieldMarshal));
                    }
                }
            }
            else
            {
                // parameters
                foreach (var methodHandle in metadataReader.MethodDefinitions)
                {
                    var methodDef = metadataReader.GetMethodDefinition(methodHandle);
                    string memberName = metadataReader.GetString(methodDef.Name);
                    foreach (var paramHandle in methodDef.GetParameters())
                    {
                        var paramRow = metadataReader.GetParameter(paramHandle);
                        string paramName = metadataReader.GetString(paramRow.Name);

                        byte[] expectedBlob = getExpectedBlob(memberName + ":" + paramName, assembly);
                        if (expectedBlob != null)
                        {
                            Assert.NotEqual(0, (int)(paramRow.Attributes & ParameterAttributes.HasFieldMarshal));
                            expectedMarshalCount++;

                            BlobHandle descriptor = metadataReader.GetParameter(paramHandle).GetMarshallingDescriptor();
                            Assert.False(descriptor.IsNil, "Expecting record in FieldMarshal table");

                            byte[] actualBlob = metadataReader.GetBlobBytes(descriptor);

                            AssertEx.Equal(expectedBlob, actualBlob);
                        }
                        else
                        {
                            Assert.Equal(0, (int)(paramRow.Attributes & ParameterAttributes.HasFieldMarshal));
                        }
                    }
                }
            }

            Assert.Equal(expectedMarshalCount, metadataReader.GetTableRowCount(TableIndex.FieldMarshal));
        }

        internal static IEnumerable<string> GetFullTypeNames(MetadataReader metadataReader)
        {
            foreach (var typeDefHandle in metadataReader.TypeDefinitions)
            {
                var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                var ns = metadataReader.GetString(typeDef.Namespace);
                var name = metadataReader.GetString(typeDef.Name);

                yield return (ns.Length == 0) ? name : (ns + "." + name);
            }
        }

        internal static IEnumerable<string> GetExportedTypesFullNames(MetadataReader metadataReader)
        {
            foreach (var typeDefHandle in metadataReader.ExportedTypes)
            {
                var typeDef = metadataReader.GetExportedType(typeDefHandle);
                var ns = metadataReader.GetString(typeDef.Namespace);
                var name = metadataReader.GetString(typeDef.Name);

                yield return (ns.Length == 0) ? name : (ns + "." + name);
            }
        }

        public static void VerifyMetadataEqualModuloMvid(Stream peStream1, Stream peStream2)
        {
            peStream1.Position = 0;
            peStream2.Position = 0;

            var peReader1 = new PEReader(peStream1);
            var peReader2 = new PEReader(peStream2);

            var md1 = peReader1.GetMetadata().GetContent();
            var md2 = peReader2.GetMetadata().GetContent();

            var mdReader1 = peReader1.GetMetadataReader();
            var mdReader2 = peReader2.GetMetadataReader();

            var mvidIndex1 = mdReader1.GetModuleDefinition().Mvid;
            var mvidIndex2 = mdReader2.GetModuleDefinition().Mvid;

            var mvidOffset1 = mdReader1.GetHeapMetadataOffset(HeapIndex.Guid) + 16 * (MetadataTokens.GetHeapOffset(mvidIndex1) - 1);
            var mvidOffset2 = mdReader2.GetHeapMetadataOffset(HeapIndex.Guid) + 16 * (MetadataTokens.GetHeapOffset(mvidIndex2) - 1);

            if (!md1.RemoveRange(mvidOffset1, 16).SequenceEqual(md1.RemoveRange(mvidOffset2, 16)))
            {
                var mdw1 = new StringWriter();
                var mdw2 = new StringWriter();
                new MetadataVisualizer(mdReader1, mdw1).Visualize();
                new MetadataVisualizer(mdReader2, mdw2).Visualize();
                mdw1.Flush();
                mdw2.Flush();

                AssertEx.AssertResultsEqual(mdw1.ToString(), mdw2.ToString());
            }
        }
    }
}
