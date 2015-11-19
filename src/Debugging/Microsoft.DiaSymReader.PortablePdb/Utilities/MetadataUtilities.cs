// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal static class MetadataUtilities
    {
        public const SignatureTypeCode SignatureTypeCode_ValueType = (SignatureTypeCode)0x11;
        public const SignatureTypeCode SignatureTypeCode_Class = (SignatureTypeCode)0x12;
        public static int MethodDefToken(int rowId) => 0x06000000 | rowId;

        // Custom Attribute kinds:
        public static readonly Guid MethodSteppingInformationBlobId = new Guid("54FD2AC5-E925-401A-9C2A-F94F171072F8");
        public static readonly Guid VbDefaultNamespaceId = new Guid("58b2eab6-209f-4e4e-a22c-b2d0f910c782");

        internal static int GetTypeDefOrRefOrSpecCodedIndex(EntityHandle typeHandle)
        {
            int tag = 0;
            switch (typeHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    tag = 0;
                    break;

                case HandleKind.TypeReference:
                    tag = 1;
                    break;

                case HandleKind.TypeSpecification:
                    tag = 2;
                    break;
            }

            return (MetadataTokens.GetRowNumber(typeHandle) << 2) | tag;
        }

        internal static BlobHandle GetCustomDebugInformation(this MetadataReader reader, EntityHandle parent, Guid kind)
        {
            foreach (var cdiHandle in reader.GetCustomDebugInformation(parent))
            {
                var cdi = reader.GetCustomDebugInformation(cdiHandle);
                if (reader.GetGuid(cdi.Kind) == kind)
                {
                    // return the first record
                    return cdi.Value;
                }
            }

            return default(BlobHandle);
        }
    }
}