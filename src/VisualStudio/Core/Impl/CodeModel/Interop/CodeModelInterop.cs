﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
