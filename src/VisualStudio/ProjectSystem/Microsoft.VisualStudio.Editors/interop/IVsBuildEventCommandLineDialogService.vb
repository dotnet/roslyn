Imports System
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.Interop

    <ComImport(), System.Runtime.InteropServices.Guid("A0EBEE86-72AD-4a29-8C0E-D745F843BE1D"), _
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsDual), _
    CLSCompliant(False)> _
    Friend Interface IVsBuildEventCommandLineDialogService
        <PreserveSig()> _
        Function EditCommandLine(<InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal WindowText As String, _
        <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal HelpID As String, _
        <InAttribute(), MarshalAs(UnmanagedType.BStr)> ByVal OriginalCommandLine As String, _
        <InAttribute()> ByVal MacroProvider As IVsBuildEventMacroProvider, _
        <OutAttribute(), MarshalAs(UnmanagedType.BStr)> ByRef Result As String) As Integer
    End Interface

End Namespace
