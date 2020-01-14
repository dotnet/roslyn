// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// Minimal implementation of IMetadataImport that implements APIs used by SymReader and SymWriter.
    /// </summary>
    internal unsafe sealed class SymWriterMetadataAdapter : MetadataAdapterBase
    {
        private readonly ISymWriterMetadataProvider _metadataProvider;

        public SymWriterMetadataAdapter(ISymWriterMetadataProvider metadataProvider)
        {
            Debug.Assert(metadataProvider != null);
            _metadataProvider = metadataProvider;
        }

        public override int GetTokenFromSig(byte* voidPointerSig, int byteCountSig)
        {
            // Only used when building constant signature. 
            // We trick SymWriter into embedding NIL token into the PDB if 
            // we don't have a real signature token matching the constant type.
            return 0x11000000;
        }

        public override int GetTypeDefProps(
            int typeDef,
            [Out]char* qualifiedName,
            int qualifiedNameBufferLength,
            [Out]int* qualifiedNameLength,
            [Out]TypeAttributes* attributes,
            [Out]int* baseType)
        {
            Debug.Assert(baseType == null);

            if (!_metadataProvider.TryGetTypeDefinitionInfo(typeDef, out var namespaceName, out var typeName, out var attrib))
            {
                return HResult.E_INVALIDARG;
            }

            if (qualifiedNameLength != null || qualifiedName != null)
            {
                InteropUtilities.CopyQualifiedTypeName(
                    qualifiedName,
                    qualifiedNameBufferLength,
                    qualifiedNameLength,
                    namespaceName,
                    typeName);
            }

            if (attributes != null)
            {
                *attributes = attrib;
            }

            return HResult.S_OK;
        }

        public override int GetTypeRefProps(
            int typeRef,
            [Out]int* resolutionScope, // ModuleRef or AssemblyRef
            [Out]char* qualifiedName,
            int qualifiedNameBufferLength,
            [Out]int* qualifiedNameLength)
            => throw new NotImplementedException();

        public override int GetNestedClassProps(int nestedClass, out int enclosingClass)
        {
            return _metadataProvider.TryGetEnclosingType(nestedClass, out enclosingClass) ? HResult.S_OK : HResult.E_FAIL;
        }

        // The only purpose of this method is to get type name of the method and declaring type token (opaque for SymWriter), everything else is ignored by the SymWriter.
        // "mb" is the token passed to OpenMethod. The token is remembered until the corresponding CloseMethod, which passes it to GetMethodProps.
        // It's opaque for SymWriter.
        public override int GetMethodProps(
            int methodDef,
            [Out] int* declaringTypeDef,
            [Out] char* name,
            int nameBufferLength,
            [Out] int* nameLength,
            [Out] MethodAttributes* attributes,
            [Out] byte** signature,
            [Out] int* signatureLength,
            [Out] int* relativeVirtualAddress,
            [Out] MethodImplAttributes* implAttributes)
        {
            Debug.Assert(attributes == null);
            Debug.Assert(signature == null);
            Debug.Assert(signatureLength == null);
            Debug.Assert(relativeVirtualAddress == null);
            Debug.Assert(implAttributes == null);

            if (!_metadataProvider.TryGetMethodInfo(methodDef, out var nameStr, out var declaringTypeToken))
            {
                return HResult.E_INVALIDARG;
            }

            if (name != null || nameLength != null)
            {
                // if the buffer is too small to fit the name, truncate the name.
                // -1 to account for a NUL terminator.
                int adjustedLength = Math.Min(nameStr.Length, nameBufferLength - 1);

                if (nameLength != null)
                {
                    // return the length of the possibly truncated name not including NUL
                    *nameLength = adjustedLength;
                }

                if (name != null && nameBufferLength > 0)
                {
                    char* dst = name;

                    for (int i = 0; i < adjustedLength; i++)
                    {
                        *dst = nameStr[i];
                        dst++;
                    }

                    *dst = '\0';
                }
            }

            if (declaringTypeDef != null)
            {
                *declaringTypeDef = declaringTypeToken;
            }

            return HResult.S_OK;
        }
    }
}
