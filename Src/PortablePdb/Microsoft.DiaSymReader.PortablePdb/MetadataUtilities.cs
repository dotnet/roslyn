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

        internal static int GetTypeDefOrRefOrSpecCodedIndex(Handle typeHandle)
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
    }
}
