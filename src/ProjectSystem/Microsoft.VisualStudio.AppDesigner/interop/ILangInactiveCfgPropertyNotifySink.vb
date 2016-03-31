Imports System.Runtime.InteropServices

Namespace Microsoft.VisualStudio.Editors.AppDesInterop


    <ComImport(), System.Runtime.InteropServices.Guid("20bd130e-bcd6-4977-a7da-121555dca33b"), _
    InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown), _
    CLSCompliant(False), ComVisible(False)> _
    Public Interface ILangInactiveCfgPropertyNotifySink

        <PreserveSig()> _
        Function OnChanged(ByVal dispid As Integer, <MarshalAs(UnmanagedType.LPWStr)> ByVal wszConfigName As String) As Integer

    End Interface

End Namespace
