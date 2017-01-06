// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis;
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
    }
}
