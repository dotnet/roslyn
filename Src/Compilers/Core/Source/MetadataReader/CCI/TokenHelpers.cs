//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.CodeAnalysis.MetadataReader.PEFileFlags;
using Microsoft.CodeAnalysis.Text;
using mdToken = System.UInt32;
using RowId = System.UInt32;

namespace Microsoft.CodeAnalysis.MetadataReader.PEFile
{
    internal static class TokenHelpers
    {
        internal static RowId RidFromToken(mdToken token)
        {
            return token & TokenTypeIds.RIDMask;
        }

        // See TokenTypeIds class
        internal static uint TypeFromToken(mdToken token)
        {
            return token & TokenTypeIds.TokenTypeMask;
        }

        internal static bool IsNilToken(mdToken tk)
        {
            return RidFromToken(tk) == 0;
        }

        internal static mdToken TokenFromRid(RowId rid, uint tktype)
        {
            return rid | tktype;
        }
    }
}