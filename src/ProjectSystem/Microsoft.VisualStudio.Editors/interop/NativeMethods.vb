Imports System
Imports System.Runtime.InteropServices
Imports System.Security.Permissions
Imports System.Text
Imports Microsoft.VisualStudio.OLE.Interop
Imports ComTypes = System.Runtime.InteropServices.ComTypes

Namespace Microsoft.VisualStudio.Editors.Interop
    <ComVisible(False)> _
    Friend NotInheritable Class NativeMethods

        Private Const VB_COMPILER_GUID As String = "019971d6-4685-11d2-b48a-0000f87572eb"
        Friend Shared ReadOnly VBCompilerGuid As System.Guid = New System.Guid(VB_COMPILER_GUID)

        '/ <summary>
        '/     Handle type for HDC's that count against the Win98 limit of five DC's.  HDC's
        '/     which are not scarce, such as HDC's for bitmaps, are counted as GDIHANDLE's.
        '/ </summary>
        Friend Shared InvalidIntPtr As IntPtr = New IntPtr(-1)
        Friend Const S_OK As Integer = &H0
        Friend Const S_FALSE As Integer = &H1
        Friend Const E_UNEXPECTED As Integer = &H8000FFFF
        Friend Const E_NOTIMPL As Integer = &H80004001
        Friend Const E_OUTOFMEMORY As Integer = &H8007000E
        Friend Const E_INVALIDARG As Integer = &H80070057
        Friend Const E_NOINTERFACE As Integer = &H80004002
        Friend Const E_POINTER As Integer = &H80004003
        Friend Const E_HANDLE As Integer = &H80070006
        Friend Const E_ABORT As Integer = &H80004004
        Friend Const E_FAIL As Integer = &H80004005
        Friend Const E_ACCESSDENIED As Integer = &H80070005
        Friend Const E_PENDING As Integer = &H8000000A

        Friend Const VS_E_INCOMPATIBLEDOCDATA As Integer = &H80041FEA
        Friend Const VS_E_UNSUPPORTEDFORMAT As Integer = &H80041FEB
        Friend Const OLECMDERR_E_NOTSUPPORTED As Integer = &H80040100
        Friend Const OLECMDERR_E_CANCELED As Integer = &H80040103
        Friend Const OLECMDERR_E_UNKNOWNGROUP As Integer = &H80040104

        Friend Shared ReadOnly IID_IMetaDataImport As Guid = New Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")

        ' Tree view and List view consts
        Friend Const TV_FIRST As Integer = &H1100
        Friend Const TVM_SETEXTENDEDSTYLE As Integer = TV_FIRST + 44
        Friend Const TVM_GETEXTENDEDSTYLE As Integer = TV_FIRST + 45
        Friend Const TVM_SETAUTOSCROLLINFO As Integer = TV_FIRST + 59
        Friend Const TVS_NOHSCROLL As Integer = &H8000
        Friend Const TVS_HASLINES As Integer = &H2
        Friend Const TVS_EX_AUTOHSCROLL As Integer = &H20
        Friend Const TVS_EX_DOUBLEBUFFER As Integer = &H4
        Friend Const TVS_EX_FADEINOUTEXPANDOS As Integer = &H40
        Friend Const TVS_TRACKSELECT As Integer = &H200

        Friend Const LVM_FIRST As Integer = &H1000     ' ListView messages
        Friend Const LVS_EX_DOUBLEBUFFER As Integer = &H10000
        Friend Const LVM_SETEXTENDEDLISTVIEWSTYLE As Integer = (LVM_FIRST + 54)
        Friend Const LVM_GETEXTENDEDLISTVIEWSTYLE As Integer = (LVM_FIRST + 55)

        Friend Shared Function Succeeded(ByVal hr As Integer) As Boolean
            Return hr >= 0
        End Function 'Succeeded

        Friend Shared Function Failed(ByVal hr As Integer) As Boolean
            Return hr < 0
        End Function 'Failed

        Friend Shared Function HRESULT_FROM_WIN32(ByVal x As Integer) As Integer
            If x <> 0 Then
                Return (x And &H0000FFFF) Or (Interop.win.FACILITY_WIN32 * &H10000) Or &H80000000
            Else
                Return 0
            End If
        End Function ' HRESULT_FROM_WIN32

        '    Friend Const HWND_TOP As Integer = 0
        '    Friend Const HWND_BOTTOM As Integer = 1
        '    Friend Const HWND_TOPMOST As Integer = -1
        '    Friend Const HWND_NOTOPMOST As Integer = -2

        Friend Shared IID_IUnknown As New Guid("{00000000-0000-0000-C000-000000000046}")
        'Friend Shared IID_IDispatch As New Guid("{00000000-0000-0000-C000-000000000046}")

        Friend Const WM_KEYDOWN As Integer = &H100
        'Friend Const WM_KEYUP As Integer = &H101
        Friend Const WM_CHAR As Integer = &H102
        'Friend Const WM_DEADCHAR As Integer = &H103
        'Friend Const WM_CTLCOLOR As Integer = &H19
        'Friend Const WM_SYSKEYDOWN As Integer = &H104
        'Friend Const WM_SYSKEYUP As Integer = &H105
        'Friend Const WM_SYSCHAR As Integer = &H106
        Friend Const WM_SETREDRAW as Integer = &HB
        Friend Const LVM_SETCOLUMNWIDTH As Integer = (&H1000 + 30)
        Friend Const LVSCW_AUTOSIZE As Integer = -1
        Friend Const LVSCW_AUTOSIZE_USEHEADER As Integer = -2


        Friend Class ConnectionPointCookie
            Private connectionPoint As IConnectionPoint
            Private connectionPoint2 As ComTypes.IConnectionPoint
            Private cookie As UInteger
#If DEBUG Then
            Private callStack As String
            Private eventInterface As Type
#End If


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.ConnectionPointCookie"]/*' />
            '/ <devdoc>
            '/ Creates a connection point to of the given interface type.
            '/ which will call on a managed code sink that implements that interface.
            '/ </devdoc>
            Friend Sub New(ByVal [source] As Object, ByVal sink As Object, ByVal eventInterface As Type)
                MyClass.New([source], sink, eventInterface, True)
            End Sub 'New


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.ConnectionPointCookie1"]/*' />
            '/ <devdoc>
            '/ Creates a connection point to of the given interface type.
            '/ which will call on a managed code sink that implements that interface.
            '/ </devdoc>
            Friend Sub New(ByVal [source] As Object, ByVal sink As Object, ByVal eventInterface As Type, ByVal throwException As Boolean)
                Dim ex As Exception = Nothing
                If sink Is Nothing OrElse Not eventInterface.IsInstanceOfType(sink) Then
                    ex = New InvalidCastException("The sink object does not implement the eventInterface.")
                ElseIf TypeOf [source] Is IConnectionPointContainer Then
                    Dim cpc As IConnectionPointContainer = CType([source], IConnectionPointContainer)

                    Try
                        Dim tmp As Guid = eventInterface.GUID
                        cpc.FindConnectionPoint(tmp, connectionPoint)
                    Catch
                        connectionPoint = Nothing
                    End Try

                    If connectionPoint Is Nothing Then
                        ex = New ArgumentException(String.Format("The source object does not expose the {0} event inteface.", eventInterface.Name))
                    Else
                        Try
                            connectionPoint.Advise(sink, cookie)
                        Catch e As Exception
                            cookie = 0
                            connectionPoint = Nothing
                            ex = New Exception(String.Format("IConnectionPoint::Advise failed for event interface '{0}'", eventInterface.Name))
                        End Try
                    End If
                ElseIf TypeOf [source] Is ComTypes.IConnectionPointContainer Then
                    Dim cpc As ComTypes.IConnectionPointContainer = CType([source], ComTypes.IConnectionPointContainer)

                    Try
                        Dim tmp As Guid = eventInterface.GUID
                        cpc.FindConnectionPoint(tmp, connectionPoint2)
                    Catch
                        connectionPoint2 = Nothing
                    End Try

                    If connectionPoint2 Is Nothing Then
                        ex = New ArgumentException(String.Format("The source object does not expose the {0} event inteface.", eventInterface.Name))
                    Else
                        Dim cookie2 As Integer
                        Try
                            connectionPoint2.Advise(sink, cookie2)
                        Catch e As Exception
                            connectionPoint2 = Nothing
                            ex = New Exception(String.Format("IConnectionPoint::Advise failed for event interface '{0}'", eventInterface.Name))
                        End Try
                        cookie = CUInt(cookie2)
                    End If
                    ex = New InvalidCastException("The source object does not expose IConnectionPointContainer.")
                End If


                If throwException AndAlso (connectionPoint Is Nothing OrElse cookie = 0) Then
                    If ex Is Nothing Then
                        Throw New ArgumentException(String.Format("Could not create connection point for event interface '{0}'", eventInterface.Name))
                    Else
                        Throw ex
                    End If
                End If

#If DEBUG Then
                callStack = Environment.StackTrace
                Me.eventInterface = eventInterface
#End If
            End Sub 'New


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.Disconnect"]/*' />
            '/ <devdoc>
            '/ Disconnect the current connection point.  If the object is not connected,
            '/ this method will do nothing.
            '/ </devdoc>
            Friend Overloads Sub Disconnect()
                Disconnect(False)
            End Sub 'Disconnect


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.Disconnect1"]/*' />
            '/ <devdoc>
            '/ Disconnect the current connection point.  If the object is not connected,
            '/ this method will do nothing.
            '/ </devdoc>
            Friend Overloads Sub Disconnect(ByVal release As Boolean)
                If cookie <> 0 Then
                    Try
                        If connectionPoint IsNot Nothing Then
                            connectionPoint.Unadvise(cookie)

                            If release Then
                                Marshal.ReleaseComObject(connectionPoint)
                            End If
                            connectionPoint = Nothing
                        ElseIf connectionPoint2 IsNot Nothing Then
                            connectionPoint2.Unadvise(CInt(cookie))

                            If release Then
                                Marshal.ReleaseComObject(connectionPoint2)
                            End If
                            connectionPoint2 = Nothing
                        End If
                    Finally
                        cookie = 0
                        GC.SuppressFinalize(Me)
                    End Try
                End If
            End Sub 'Disconnect


            '/ <include file='doc\NativeMethods.uex' path='docs/doc[@for="NativeMethods.ConnectionPointCookie.Finalize"]/*' />
            '/ <internalonly/>
            Protected Overrides Sub Finalize()

#If DEBUG Then
                System.Diagnostics.Debug.Assert(cookie = 0, "We should never finalize an active connection point. (Interface = " & eventInterface.FullName & "), allocating code (see stack) is responsible for unhooking the ConnectionPoint by calling Disconnect.  Hookup Stack =" & Microsoft.VisualBasic.vbNewLine &  callStack)
#End If
                ' We can't call Disconnect here, because connectionPoint could be finalized earlier
                MyBase.Finalize()

            End Sub 'Finalize

        End Class 'ConnectionPointCookie

	' ClickOnce signing page native methods and structs

	Friend Enum KeySpec
	    ' Fields	
	    AT_KEYEXCHANGE = 1
	    AT_SIGNATURE = 2
	End Enum

	 <StructLayout(LayoutKind.Sequential)> _
	Friend Structure CRYPTOAPI_BLOB
	    Friend cbData As UInt32
	    Friend pbData As IntPtr
	End Structure

 
	<DllImport("crypt32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)> _
	Friend Shared Function PFXImportCertStore(<[In]> ByVal blob As IntPtr, <[In]> ByVal password As String, <[In]> ByVal flags As CryptFlags) As IntPtr
	End Function
 
	<Flags> _
	Friend Enum CryptFlags
	    ' Fields
	    Exportable = 1
	    MachineKeySet = &H20
	    UserKeySet = &H1000
	    UserProtected = 2
	End Enum

	<DllImport("crypt32.dll", SetLastError:=True)> _
	Friend Shared Function CertEnumCertificatesInStore(<[In]> ByVal CertStore As IntPtr, <[In]> ByVal PrevCertContext As IntPtr) As IntPtr
	End Function

	<DllImport("crypt32.dll", SetLastError:=True)> _
	Friend Shared Function CryptAcquireCertificatePrivateKey(<[In]> ByVal CertContext As IntPtr, <[In]> ByVal flags As UInt32, <[In]> ByVal reserved As IntPtr, <[In], Out> ByRef CryptProv As IntPtr, <[In], Out> ByRef KeySpec As KeySpec, <[In], Out, MarshalAs(UnmanagedType.Bool)> ByRef CallerFreeProv As Boolean) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("advapi32.dll", SetLastError:=True)> _
	Friend Shared Function CryptGetUserKey(<[In]> ByVal CryptProv As IntPtr, <[In]> ByVal KeySpec As KeySpec, <[In], Out> ByRef Key As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("advapi32.dll", SetLastError:=True)> _
	Friend Shared Function CryptExportKey(<[In]> ByVal Key As IntPtr, <[In]> ByVal ExpKey As IntPtr, <[In]> ByVal type As BlobType, <[In]> ByVal Flags As UInt32, <[In]> ByVal Data As IntPtr, <[In], Out> ByRef DataLen As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	Friend Enum BlobType
	    ' Fields
	    OPAQUEKEYBLOB = 9
	    PLAINTEXTKEYBLOB = 8
	    PRIVATEKEYBLOB = 7
	    PUBLICKEYBLOB = 6
	    PUBLICKEYBLOBEX = 10
	    SIMPLEBLOB = 1
	    SYMMETRICWRAPKEYBLOB = 11
	End Enum

	<DllImport("advapi32.dll", SetLastError:=True)> _
	Friend Shared Function CryptDestroyKey(ByVal hKey As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("advapi32.dll", SetLastError:=True)> _
	Friend Shared Function CryptReleaseContext(<[In]> ByVal Prov As IntPtr, <[In]> ByVal Flags As UInt32) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("crypt32.dll", SetLastError:=True)> _
	Friend Shared Function CertFreeCertificateContext(ByVal CertContext As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

	<DllImport("crypt32.dll", SetLastError:=True)> _
	Friend Shared Function CertCloseStore(<[In]> ByVal CertStore As IntPtr, ByVal Flags As CertStoreClose) As <MarshalAs(UnmanagedType.Bool)> Boolean
	End Function

 
	<Flags> _
	Friend Enum CertStoreClose
	    ' Fields
	    CERT_CLOSE_STORE_CHECK_FLAG = 2
	    CERT_CLOSE_STORE_FORCE_FLAG = 1
	End Enum

       <PreserveSig()> Friend Declare Function _
            SetParent _
                Lib "user32" (ByVal hwnd As IntPtr, ByVal hWndParent As IntPtr) As IntPtr


        <PreserveSig()> Friend Declare Function _
            GetParent _
                Lib "user32" (ByVal hwnd As IntPtr) As IntPtr

        <PreserveSig()> Friend Declare Function _
            GetFocus _
                Lib "user32" () As IntPtr
        
        <PreserveSig()> Friend Declare Function _
            SetFocus _
                Lib "user32" (ByVal hwnd As IntPtr) As Integer

        <PreserveSig()> Friend Declare Auto Function _
            SendMessage _
                Lib "user32" (ByVal hwnd As HandleRef, ByVal msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As IntPtr

        <PreserveSig()> Friend Declare Auto Function _
            SendMessage _
                Lib "user32" (ByVal hwnd As HandleRef, ByVal msg As Integer, ByVal wParam As Integer, ByRef lParam As TVITEM) As IntPtr

        <PreserveSig()> Friend Declare Auto Function _
            SendMessage _
                Lib "user32" (ByVal hwnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr

        <PreserveSig()> Friend Declare Auto Function _
            PostMessage _
                Lib "user32" (ByVal hwnd As IntPtr, ByVal msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As IntPtr

        <PreserveSig()> Friend Declare Auto Function _
            WaitMessage _
                Lib "user32" () As Boolean

        ''' <summary>
        ''' The GetNextDlgTabItem function retrieves a handle to the first control that has the WS_TABSTOP style that precedes (or follows) the specified control. 
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        <PreserveSig()> Friend Declare Auto Function _
            GetNextDlgTabItem _
                Lib "user32" (ByVal hDlg As IntPtr, ByVal hCtl As IntPtr, ByVal bPrevious As Boolean) As IntPtr


        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function GetWindow Lib "user32" (ByVal Hwnd As IntPtr, ByVal uCmd As UInteger) As IntPtr

        <PreserveSig()> _
        Friend Declare Auto Function DragQueryFile Lib "shell32" (ByVal hDrop As IntPtr, ByVal iFile As Integer, ByVal lpszFile As String, ByVal cch As Integer) As Integer

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Function GetUserDefaultLCID Lib "kernel32" () As UInteger

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Function GetTopWindow Lib "user32" (ByVal Hwnd As IntPtr) As IntPtr

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function SetWindowLong Lib "user32" (ByVal hWnd As IntPtr, ByVal Index As Integer, ByVal Value As IntPtr) As IntPtr
        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function GetWindowLong Lib "user32" (ByVal Hwnd As IntPtr, ByVal Index As Integer) As IntPtr

        ' Windows theme
        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function SetWindowTheme Lib "uxtheme" (ByVal Hwnd As IntPtr, ByVal appName As String, ByVal subIdList As String) As Integer

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function GetWindowText Lib "user32" (ByVal hWnd As IntPtr, ByVal lpString As String, ByVal nMaxCount As Integer) As Integer

        <DllImport("user32", CharSet:=CharSet.Auto)> _
        Friend Shared Function GetWindowRect(ByVal hwnd As IntPtr, ByRef rect As RECT) As Integer
        End Function

        Friend Declare Function MoveWindow Lib "user32" _
          (ByVal hWnd As IntPtr, _
            ByVal x As Integer, ByVal y As Integer, _
            ByVal nWidth As Integer, _
            ByVal nHeight As Integer, _
            ByVal bRepaint As Integer) As Integer

        <StructLayout(LayoutKind.Sequential)> _
        Friend Structure RECT
            Public left As Integer
            Public top As Integer
            Public right As Integer
            Public bottom As Integer
        End Structure

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function IsChild Lib "user32" (ByVal hWndParent As IntPtr, ByVal hWnd As IntPtr) As Boolean

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function EnableWindow Lib "user32" (ByVal hWnd As IntPtr, ByVal bEnable As Boolean) As Boolean

        '<System.Runtime.InteropServices.PreserveSig()> _
        'Friend Declare Auto Sub ShowWindow Lib "user32" (ByVal Hwnd As IntPtr, ByVal Flags As Integer)
        '
        '<System.Runtime.InteropServices.PreserveSig()> _
        'Friend Declare Auto Function SetWindowPos Lib "user32" (ByVal Hwnd As IntPtr, ByVal HwndInsertAfter As IntPtr, ByVal x As Integer, _
        '    ByVal y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal flags As Integer) As Boolean

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function SystemParametersInfo Lib "user32" (ByVal uiAction As UInteger, ByVal uiParam As UInteger, ByVal pvParam As IntPtr, ByVal fWinIni As UInteger) As Integer

        <System.Runtime.InteropServices.PreserveSig()> _
        Friend Declare Auto Function MsgWaitForMultipleObjects Lib "user32" (ByVal nCount As Integer, ByVal pHandles As IntPtr, ByVal fWaitAll As Boolean, ByVal dwMilliSeconds As Integer, ByVal dwWakeMask As Integer) As Integer

        Friend Const GWL_EXSTYLE As Integer = -20
        Friend Const GWL_STYLE As Integer = -16
        Friend Const GWL_WNDPROC As Integer = -4
        Friend Const GWL_HINSTANCE As Integer = -6
        Friend Const GWL_ID As Integer = -12
        Friend Const GWL_USERDATA As Integer = -21
        Friend Const WS_EX_CONTROLPARENT As Integer = &H10000
        Friend Const WS_TABSTOP As Integer = &H10000
        Friend Const DS_CONTROL As Integer = &H400
        Friend Declare Auto Function IsValidCodePage Lib "kernel32" (ByVal CodePage As UInteger) As Boolean

        Friend Declare Function IsWindowUnicode Lib "user32" (ByVal hWnd As IntPtr) As Boolean

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
        Friend Shared Function GetComboBoxInfo(ByVal hwndCombo As IntPtr, ByRef info As COMBOBOXINFO) As Boolean
        End Function

        <StructLayout(LayoutKind.Sequential)> _
        Friend Structure COMBOBOXINFO
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
    Friend Interface ILangPropertyProvideBatchUpdate
        Sub BeginBatch()
        Sub EndBatch()
        Sub IsBatchModeEnabled(<InAttribute(), Out()> ByRef BatchModeEnabled As Boolean)
        Sub PushOptionsToCompiler(ByVal dispid As UInteger)
    End Interface

    <ComImport()> _
    <Guid("E5CB7A31-7512-11d2-89CE-0080C792E5D8")> _
    <TypeLibType(TypeLibTypeFlags.FCanCreate)> _
    <ClassInterface(ClassInterfaceType.None)> _
    Friend Class CorMetaDataDispenser
    End Class

    <ComImport()> _
    <Guid("809c652e-7396-11d2-9771-00a0c9b4d50c")> _
    <InterfaceType(ComInterfaceType.InterfaceIsIUnknown)> _
    <TypeLibType(TypeLibTypeFlags.FRestricted)> _
    Friend Interface IMetaDataDispenser
        Function DefineScope(<[In]()> ByRef rclsid As Guid, <[In]()> ByVal dwCreateFlags As UInt32, <[In]()> ByRef riid As Guid) As <MarshalAs(UnmanagedType.Interface)> Object
        <PreserveSig()> Function OpenScope(<[In](), MarshalAs(UnmanagedType.LPWStr)> ByVal szScope As String, <[In]()> ByVal dwOpenFlags As UInt32, <[In]()> ByRef riid As Guid, <Out(), MarshalAs(UnmanagedType.Interface)> ByRef obj As Object) As Integer
        Function OpenScopeOnMemory(<[In]()> ByVal pData As IntPtr, <[In]()> ByVal cbData As UInt32, <[In]()> ByVal dwOpenFlags As UInt32, <[In]()> ByRef riid As Guid) As <MarshalAs(UnmanagedType.Interface)> Object
    End Interface
 
    <StructLayout(LayoutKind.Sequential)> _
    Friend Class HDITEM2
        Public mask As Integer = 0
        Public cxy As Integer = 0
        Public pszTextPtr As IntPtr
        Public hbm As IntPtr = IntPtr.Zero
        Public cchTextMax As Integer = 0
        Public fmt As Integer = 0
        Public lParam As IntPtr = IntPtr.Zero
        Public iImage As Integer = 0
        Public iOrder As Integer = 0
        Public type As Integer = 0
        Public pvFilter As IntPtr = IntPtr.Zero
    End Class

End Namespace
