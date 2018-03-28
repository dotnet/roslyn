// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    internal static class CodeModelInterop
    {
        [DllImport("oleaut32.dll")]
        private static extern int VariantChangeType(
            [MarshalAs(UnmanagedType.Struct)] out object pvargDest,
            [In, MarshalAs(UnmanagedType.Struct)] ref object pvargSrc,
            ushort wFlags,
            VarEnum vt);

        public static bool CanChangedVariantType(object source, VarEnum variantType)
        {
            return ErrorHandler.Succeeded(VariantChangeType(out var result, ref source, 0, variantType));
        }
    }
}
