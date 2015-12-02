Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport(), System.Runtime.InteropServices.Guid("3EB048DA-F881-4a7f-A9D4-0258E19978AA"), _
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), _
    CLSCompliant(False)> _
    Friend Interface IVBEntryPointProvider
        'Lists all Form classes with an entry point. If called with cItems = 0 and
        '  pcActualItems != NULL, GetEntryPointsList returns in pcActualItems the number
        '  of items available. When called with cItems != 0, GetEntryPointsList assumes
        '  that there is enough space in strList[] for that many items, and fills up the
        '  array with those items (up to maximum available).  Returns in pcActualItems 
        '  the actual number of items that could be put in the array (this can be greater than or 
        '  less than cItems). Assumes that the caller takes care of array allocation and de-allocation.
        '        Function GetFormEntryPointsList(ByVal pHierarchy As Object, _
        '                                       ByVal cItems As UInteger, _
        '                                      <MarshalAs(UnmanagedType.LPArray, arraysubtype:=UnmanagedType.BStr), [In](), Out()> ByRef c() As String, _
        '                                     <Out()> ByRef pcActualItems As UInteger) As Integer
        Function GetFormEntryPointsList(<MarshalAs(UnmanagedType.IUnknown), [In]()> ByVal pHierarchy As Object, _
                                        ByVal cItems As UInteger, _
                                        <Out(), MarshalAs(UnmanagedType.LPArray)> ByVal bstrList As String(), _
                                        <Out()> ByRef pcActualItems As UInteger) As Integer

    End Interface


End Namespace
