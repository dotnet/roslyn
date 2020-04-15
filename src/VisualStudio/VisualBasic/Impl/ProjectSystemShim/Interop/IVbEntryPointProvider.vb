' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
    <Guid("3EB048DA-F881-4a7f-A9D4-0258E19978AA"), ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Friend Interface IVBEntryPointProvider
        ''' <summary>
        ''' Lists all Form classes with an entry point. If called with cItems = 0 and
        '''  pcActualItems != NULL, GetEntryPointsList returns in pcActualItems the number
        '''  of items available. When called with cItems != 0, GetEntryPointsList assumes
        '''  that there is enough space in strList[] for that many items, and fills up the
        '''  array with those items (up to maximum available).  Returns in pcActualItems 
        '''  the actual number of items that could be put in the array (this can be greater than or 
        '''  less than cItems). Assumes that the caller takes care of array allocation and de-allocation.
        ''' </summary>
        Function GetFormEntryPointsList(<MarshalAs(UnmanagedType.IUnknown), [In]()> ByVal pHierarchy As Object,
                                        ByVal cItems As Integer,
                                        <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=1)> ByVal bstrList As String(),
                                        <Out()> ByVal pcActualItems As IntPtr) As Integer
    End Interface

End Namespace
