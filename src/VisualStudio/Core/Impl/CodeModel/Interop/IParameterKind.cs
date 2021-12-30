// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    internal enum PARAMETER_PASSING_MODE
    {
        cmParameterTypeIn = 1,
        cmParameterTypeOut = 2,
        cmParameterTypeInOut = 3
    }

    [ComImport]
    [Guid("A55CCBCC-7031-432d-B30A-A68DE7BDAD75")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    internal interface IParameterKind
    {
        void SetParameterPassingMode(PARAMETER_PASSING_MODE ParamPassingMode);
        void SetParameterArrayDimensions(int ulDimensions);
        int GetParameterArrayCount();
        int GetParameterArrayDimensions(int uIndex);
        PARAMETER_PASSING_MODE GetParameterPassingMode();
    }
}
