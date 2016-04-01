' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.OLE.Interop
Imports ComTypes = System.Runtime.InteropServices.ComTypes
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.AppDesInterop
    <ComVisible(False)> _
    Friend NotInheritable Class NativeMethods

        Private Const s_VB_COMPILER_GUID As String = "019971d6-4685-11d2-b48a-0000f87572eb"
        Public Shared ReadOnly VBCompilerGuid As System.Guid = New System.Guid(s_VB_COMPILER_GUID)

        '/ <summary>
        '/     Handle type for HDC's that count against the Win98 limit of five DC's.  HDC's
        '/     which are not scarce, such as HDC's for bitmaps, are counted as GDIHANDLE's.
        '/ </summary>
        Public Shared InvalidIntPtr As IntPtr = New IntPtr(-1)
        Public Const S_OK As Integer = &H0
        Public Const S_FALSE As Integer = &H1
        Public Const E_UNEXPECTED As Integer = &H8000FFFF
        Public Const E_NOTIMPL As Integer = &H80004001
        Public Const E_OUTOFMEMORY As Integer = &H8007000E
        Public Const E_INVALIDARG As Integer = &H80070057
        Public Const E_NOINTERFACE As Integer = &H80004002
        Public Const E_POINTER As Integer = &H80004003
        Public Const E_HANDLE As Integer = &H80070006
        Public Const E_ABORT As Integer = &H80004004
        Public Const E_FAIL As Integer = &H80004005
        Public Const E_ACCESSDENIED As Integer = &H80070005
        Public Const E_PENDING As Integer = &H8000000A

        Public Const VS_E_INCOMPATIBLEDOCDATA As Integer = &H80041FEA
        Public Const VS_E_UNSUPPORTEDFORMAT As Integer = &H80041FEB
        Public Const OLECMDERR_E_NOTSUPPORTED As Integer = &H80040100
        Public Const OLECMDERR_E_CANCELED As Integer = &H80040103
        Public Const OLECMDERR_E_UNKNOWNGROUP As Integer = &H80040104

        Public Shared ReadOnly IID_IMetaDataImport As Guid = New Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")


        Public Shared Function Succeeded(ByVal hr As Integer) As Boolean
            Return hr >= 0
        End Function 'Succeeded

        Public Shared Function Failed(ByVal hr As Integer) As Boolean
            Return hr < 0
        End Function 'Failed

        Public Shared Function HRESULT_FROM_WIN32(ByVal x As Integer) As Integer
            If x <> 0 Then
                Return (x And &HFFFF) Or (AppDesInterop.win.FACILITY_WIN32 * &H10000) Or &H80000000
            Else
                Return 0
            End If
        End Function ' HRESULT_FROM_WIN32

        '    Public Const HWND_TOP As Integer = 0
        '    Public Const HWND_BOTTOM As Integer = 1
        '    Public Const HWND_TOPMOST As Integer = -1
        '    Public Const HWND_NOTOPMOST As Integer = -2

        Public Shared IID_IUnknown As New Guid("{00000000-0000-0000-C000-000000000046}")
        'Public Shared IID_IDispatch As New Guid("{00000000-0000-0000-C000-000000000046}")

        Public Const WM_KEYDOWN As Integer = &H100
        'Public Const WM_KEYUP As Integer = &H101
        Public Const WM_CHAR As Integer = &H102
        'Public Const WM_DEADCHAR As Integer = &H103
        Public Const WM_UPDATEUISTATE As Integer = &H128
        'Public Const WM_CTLCOLOR As Integer = &H19
        Public Const WM_SETREDRAW As Integer = &HB
        Public Const LVM_SETCOLUMNWIDTH As Integer = (&H1000 + 30)
        Public Const LVSCW_AUTOSIZE As Integer = -1
        Public Const LVSCW_AUTOSIZE_USEHEADER As Integer = -2

        Public Const UIS_INITIALIZE As Integer = 3
        Public Const UISF_HIDEFOCUS As Integer = &H1
        Public Const UISF_HIDEACCEL As Integer = &H2

        Public Class ConnectionPointCookie
            Private _connectionPoint As IConnectionPoint
            Private _connectionPoint2 As ComTypes.IConnectionPoint
            Private _cookie As UInteger
#If DEBUG Then
            Private _callStack As String
            Private _eventInterface As Type
#End If


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.ConnectionPointCookie"]/*' />
            '/ <devdoc>
            '/ Creates a connection point to of the given interface type.
            '/ which will call on a managed code sink that implements that interface.
            '/ </devdoc>
            Public Sub New(ByVal [source] As Object, ByVal sink As Object, ByVal eventInterface As Type)
                MyClass.New([source], sink, eventInterface, True)
            End Sub 'New


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.ConnectionPointCookie1"]/*' />
            '/ <devdoc>
            '/ Creates a connection point to of the given interface type.
            '/ which will call on a managed code sink that implements that interface.
            '/ </devdoc>
            Public Sub New(ByVal [source] As Object, ByVal sink As Object, ByVal eventInterface As Type, ByVal throwException As Boolean)
                Dim ex As Exception = Nothing
                If sink Is Nothing OrElse Not eventInterface.IsInstanceOfType(sink) Then
                    ex = New InvalidCastException("The sink object does not implement the eventInterface.")
                ElseIf TypeOf [source] Is IConnectionPointContainer Then
                    Dim cpc As IConnectionPointContainer = CType([source], IConnectionPointContainer)

                    Try
                        Dim tmp As Guid = eventInterface.GUID
                        cpc.FindConnectionPoint(tmp, _connectionPoint)
                    Catch
                        _connectionPoint = Nothing
                    End Try

                    If _connectionPoint Is Nothing Then
                        ex = New ArgumentException(String.Format("The source object does not expose the {0} event inteface.", eventInterface.Name))
                    Else
                        Try
                            _connectionPoint.Advise(sink, _cookie)
                        Catch e As Exception
                            _cookie = 0
                            _connectionPoint = Nothing
                            ex = New Exception(String.Format("IConnectionPoint::Advise failed for event interface '{0}'", eventInterface.Name))
                        End Try
                    End If
                ElseIf TypeOf [source] Is ComTypes.IConnectionPointContainer Then
                    Dim cpc As ComTypes.IConnectionPointContainer = CType([source], ComTypes.IConnectionPointContainer)

                    Try
                        Dim tmp As Guid = eventInterface.GUID
                        cpc.FindConnectionPoint(tmp, _connectionPoint2)
                    Catch
                        _connectionPoint2 = Nothing
                    End Try

                    If _connectionPoint2 Is Nothing Then
                        ex = New ArgumentException(String.Format("The source object does not expose the {0} event inteface.", eventInterface.Name))
                    Else
                        Dim cookie2 As Integer
                        Try
                            _connectionPoint2.Advise(sink, cookie2)
                        Catch e As Exception
                            _connectionPoint2 = Nothing
                            ex = New Exception(String.Format("IConnectionPoint::Advise failed for event interface '{0}'", eventInterface.Name))
                        End Try
                        _cookie = CUInt(cookie2)
                    End If
                    ex = New InvalidCastException("The source object does not expose IConnectionPointContainer.")
                End If


                If throwException AndAlso ((_connectionPoint Is Nothing AndAlso _connectionPoint2 Is Nothing) OrElse _cookie = 0) Then
                    If ex Is Nothing Then
                        Throw New ArgumentException(String.Format("Could not create connection point for event interface '{0}'", eventInterface.Name))
                    Else
                        Throw ex
                    End If
                End If

#If DEBUG Then
                _callStack = Environment.StackTrace
                Me._eventInterface = eventInterface
#End If
            End Sub 'New


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.Disconnect"]/*' />
            '/ <devdoc>
            '/ Disconnect the current connection point.  If the object is not connected,
            '/ this method will do nothing.
            '/ </devdoc>
            Public Overloads Sub Disconnect()
                Disconnect(False)
            End Sub 'Disconnect


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.Disconnect1"]/*' />
            '/ <devdoc>
            '/ Disconnect the current connection point.  If the object is not connected,
            '/ this method will do nothing.
            '/ </devdoc>
            Public Overloads Sub Disconnect(ByVal release As Boolean)
                If _cookie <> 0 Then
                    Try
                        If _connectionPoint IsNot Nothing Then
                            _connectionPoint.Unadvise(_cookie)

                            If release Then
                                Marshal.ReleaseComObject(_connectionPoint)
                            End If
                            _connectionPoint = Nothing
                        ElseIf _connectionPoint2 IsNot Nothing Then
                            _connectionPoint2.Unadvise(CInt(_cookie))

                            If release Then
                                Marshal.ReleaseComObject(_connectionPoint2)
                            End If
                            _connectionPoint2 = Nothing
                        End If
                    Finally
                        _cookie = 0
                        GC.SuppressFinalize(Me)
                    End Try
                End If
            End Sub 'Disconnect


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.Finalize"]/*' />
            '/ <internalonly/>
            Protected Overrides Sub Finalize()

#If DEBUG Then
                System.Diagnostics.Debug.Assert(_cookie = 0, "We should never finalize an active connection point. (Interface = " & _eventInterface.FullName & "), allocating code (see stack) is responsible for unhooking the ConnectionPoint by calling Disconnect.  Hookup Stack =" & Microsoft.VisualBasic.vbNewLine & _callStack)
#End If
                ' We can't call Disconnect here, because connectionPoint could be finalized earlier
                MyBase.Finalize()

            End Sub 'Finalize

        End Class 'ConnectionPointCookie

        'NOTE: pcbKeyBlob is really a unsigned Integer, but we're treating as signed for ease of use
        'Public Declare Unicode Function StrongNameKeyGen Lib "mscoree.dll" (<MarshalAs(UnmanagedType.LPWStr)> ByVal wszKeyContainer As String, ByVal dwFlags As UInteger, _
        '    ByRef ppbKeyBlob As IntPtr, ByRef pcbKeyBlob As Integer) As Integer
        '
        'Public Declare Unicode Sub StrongNameFreeBuffer Lib "mscoree.dll" (ByVal ppbKeyBlob As IntPtr)

        <PreserveSig()> Public Declare Function _
            SetParent _
                Lib "user32" (ByVal hwnd As IntPtr, ByVal hWndParent As IntPtr) As IntPtr


        <PreserveSig()> Public Declare Function _
            GetParent _
                Lib "user32" (ByVal hwnd As IntPtr) As IntPtr

        <PreserveSig()> Public Declare Function _
            GetFocus _
                Lib "user32" () As IntPtr

        <PreserveSig()> Public Declare Function _
            SetFocus _
                Lib "user32" (ByVal hwnd As IntPtr) As Integer

        <PreserveSig()> Public Declare Auto Function _
            SendMessage _
                Lib "user32" (ByVal hwnd As HandleRef, ByVal msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As IntPtr

        <PreserveSig()> Public Declare Auto Function _
            SendMessage _
                Lib "user32" (ByVal hwnd As HandleRef, ByVal msg As Integer, ByVal wParam As Integer, ByRef lParam As TVITEM) As IntPtr

        <PreserveSig()> Public Declare Auto Function _
            SendMessage _
                Lib "user32" (ByVal hwnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr

        <PreserveSig()> Public Declare Auto Function _
            PostMessage _
                Lib "user32" (ByVal hwnd As IntPtr, ByVal msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As IntPtr

        <PreserveSig()> Public Declare Auto Function _
            WaitMessage _
                Lib "user32" () As Boolean

        <PreserveSig()> Public Declare Auto Function _
            IsDialogMessage _
                Lib "user32" (ByVal hwnd As HandleRef, ByRef msg As MSG) As Boolean

        <PreserveSig()> Public Declare Auto Function _
            TranslateMessage _
                Lib "user32" (ByRef msg As MSG) As Boolean

        <PreserveSig()> Public Declare Auto Function _
            GetKeyState _
                Lib "user32" (ByVal nVirtKey As Keys) As Int16

        ''' <summary>
        ''' The GetNextDlgTabItem function retrieves a handle to the first control that has the WS_TABSTOP style that precedes (or follows) the specified control. 
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <PreserveSig()> Public Declare Auto Function _
            GetNextDlgTabItem _
                Lib "user32" (ByVal hDlg As IntPtr, ByVal hCtl As IntPtr, ByVal bPrevious As Boolean) As IntPtr


        <PreserveSig()> _
        Public Declare Auto Function GetWindow Lib "user32" (ByVal Hwnd As IntPtr, ByVal uCmd As UInteger) As IntPtr

        <PreserveSig()> _
        Public Declare Auto Function DragQueryFile Lib "shell32" (ByVal hDrop As IntPtr, ByVal iFile As Integer, ByVal lpszFile As String, ByVal cch As Integer) As Integer

        <PreserveSig()> _
        Public Declare Function GetUserDefaultLCID Lib "kernel32" () As UInteger

        <PreserveSig()> _
        Public Declare Function GetTopWindow Lib "user32" (ByVal Hwnd As IntPtr) As IntPtr

        <PreserveSig()> _
        Public Declare Auto Function SetWindowLong Lib "user32" (ByVal hWnd As IntPtr, ByVal Index As Integer, ByVal Value As IntPtr) As IntPtr
        <PreserveSig()> _
        Public Declare Auto Function GetWindowLong Lib "user32" (ByVal Hwnd As IntPtr, ByVal Index As Integer) As IntPtr

        <PreserveSig()> _
        Public Declare Auto Function GetWindowText Lib "user32" (ByVal hWnd As IntPtr, ByVal lpString As String, ByVal nMaxCount As Integer) As Integer

        <DllImport("user32", CharSet:=CharSet.Auto)> _
        Public Shared Function GetWindowRect(ByVal hwnd As IntPtr, ByRef rect As RECT) As Integer
        End Function

        Public Declare Function MoveWindow Lib "user32" _
          (ByVal hWnd As IntPtr, _
            ByVal x As Integer, ByVal y As Integer, _
            ByVal nWidth As Integer, _
            ByVal nHeight As Integer, _
            ByVal bRepaint As Integer) As Integer

        <StructLayout(LayoutKind.Sequential)> _
        Public Structure RECT
            Public left As Integer
            Public top As Integer
            Public right As Integer
            Public bottom As Integer
        End Structure

        <PreserveSig()> _
        Public Declare Auto Function IsChild Lib "user32" (ByVal hWndParent As IntPtr, ByVal hWnd As IntPtr) As Boolean

        <PreserveSig()> _
        Public Declare Auto Function EnableWindow Lib "user32" (ByVal hWnd As IntPtr, ByVal bEnable As Boolean) As Boolean

        '<PreserveSig()> _
        'Public Declare Auto Sub ShowWindow Lib "user32" (ByVal Hwnd As IntPtr, ByVal Flags As Integer)
        '
        '<PreserveSig()> _
        'Public Declare Auto Function SetWindowPos Lib "user32" (ByVal Hwnd As IntPtr, ByVal HwndInsertAfter As IntPtr, ByVal x As Integer, _
        '    ByVal y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal flags As Integer) As Boolean

        <PreserveSig()> _
        Public Declare Auto Function SystemParametersInfo Lib "user32" (ByVal uiAction As UInteger, ByVal uiParam As UInteger, ByVal pvParam As IntPtr, ByVal fWinIni As UInteger) As Integer

        <PreserveSig()> _
        Public Declare Auto Function MsgWaitForMultipleObjects Lib "user32" (ByVal nCount As Integer, ByVal pHandles As IntPtr, ByVal fWaitAll As Boolean, ByVal dwMilliSeconds As Integer, ByVal dwWakeMask As Integer) As Integer

        Public Const GWL_EXSTYLE As Integer = -20
        Public Const GWL_STYLE As Integer = -16
        Public Const GWL_WNDPROC As Integer = -4
        Public Const GWL_HINSTANCE As Integer = -6
        Public Const GWL_ID As Integer = -12
        Public Const GWL_USERDATA As Integer = -21
        Public Const WS_EX_CONTROLPARENT As Integer = &H10000
        Public Const WS_TABSTOP As Integer = &H10000
        Public Const DS_CONTROL As Integer = &H400
        Public Declare Auto Function IsValidCodePage Lib "kernel32" (ByVal CodePage As UInteger) As Boolean

        Public Declare Function IsWindowUnicode Lib "user32" (ByVal hWnd As IntPtr) As Boolean

        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)> _
        Public Structure TVITEM
            Public item_mask As Integer
            Public item_hItem As IntPtr
            Public item_state As Integer
            Public item_stateMask As Integer
            Public item_pszText As IntPtr   'LPTSTR
            Public item_cchTextMax As Integer
            Public item_iImage As Integer
            Public item_iSelectedImage As Integer
            Public item_cChildren As Integer
            Public item_lParam As IntPtr
        End Structure

        <DllImport("user32")> _
        Public Shared Function GetComboBoxInfo(ByVal hwndCombo As IntPtr, ByRef info As COMBOBOXINFO) As Boolean
        End Function

        <StructLayout(LayoutKind.Sequential)> _
        Public Structure COMBOBOXINFO
            Public cbSize As Integer
            Public rcItem As RECT
            Public rcButton As RECT
            Public stateButton As IntPtr
            Public hwndCombo As IntPtr
            Public hwndEdit As IntPtr
            Public hwndList As IntPtr
        End Structure

    End Class 'NativeMethods

    '//
    '// ILangPropertyProvideBatchUpdate
    '//
    <ComImport(), Guid("F8828A38-5208-4497-991A-F8034C8D5A69"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)> _
    Public Interface ILangPropertyProvideBatchUpdate
        Sub BeginBatch()
        Sub EndBatch()
        Sub IsBatchModeEnabled(<InAttribute(), Out()> ByRef BatchModeEnabled As Boolean)
        Sub PushOptionsToCompiler(ByVal dispid As UInteger)
    End Interface

    <ComImport()> _
    <Guid("E5CB7A31-7512-11d2-89CE-0080C792E5D8")> _
    <TypeLibType(TypeLibTypeFlags.FCanCreate)> _
    <ClassInterface(ClassInterfaceType.None)> _
    Public Class CorMetaDataDispenser
    End Class

End Namespace
