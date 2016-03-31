Imports Microsoft.Win32
Imports System.Runtime.InteropServices

Namespace Microsoft.Internal.Performance

    Friend NotInheritable Class CodeMarkers

        ' Singleton access
        Public Shared ReadOnly Instance As CodeMarkers = New CodeMarkers()

        Private Class NativeMethods

            ' Add a private constructor to prevent compiler from generating a default constructor (FxCop warning CA1812)
            Private Sub New()
            End Sub

            ' ********************* Imported Win32 functions *********************
            '///// Code markers test function imports
#If Codemarkers_IncludeAppEnum Then
            <DllImport(TestDllName, EntryPoint:="InitPerf")> _
            Public Shared Sub TestDllInitPerf(ByVal iApp As System.Int32)
            End Sub

            <DllImport(TestDllName, EntryPoint:="UnInitPerf")> _
            Public Shared Sub TestDllUnInitPerf(ByVal iApp As System.Int32)
            End Sub
#End If 'Codemarkers_IncludeAppEnum           

            <DllImport(TestDllName, EntryPoint:="PerfCodeMarker")>
            Public Shared Sub TestDllPerfCodeMarker(ByVal nTimerID As System.Int32, ByVal uiLow As System.UInt32, ByVal uiHigh As System.UInt32)
            End Sub

            '///// Code markers product function imports
#If Codemarkers_IncludeAppEnum Then
            <DllImport(ProductDllName, EntryPoint:="InitPerf")> _
            Public Shared Sub ProductDllInitPerf(ByVal iApp As System.Int32)
            End Sub

            <DllImport(ProductDllName, EntryPoint:="UnInitPerf")> _
            Public Shared Sub ProductDllUnInitPerf(ByVal iApp As System.Int32)
            End Sub
#End If 'Codemarkers_IncludeAppEnum           

            <DllImport(ProductDllName, EntryPoint:="PerfCodeMarker")>
            Public Shared Sub ProductDllPerfCodeMarker(ByVal nTimerID As System.Int32, ByVal uiLow As System.UInt32, ByVal uiHigh As System.UInt32)
            End Sub

            '///// global native method imports
            <DllImport("kernel32.dll", CharSet:=CharSet.Unicode)>
            Public Shared Function FindAtom(ByVal lpString As String) As System.UInt16
            End Function

#If Codemarkers_IncludeAppEnum Then
            <DllImport("kernel32.dll", CharSet:=CharSet.Unicode)> _
            Public Shared Function AddAtom(ByVal lpString As String) As UInt16
            End Function

            <DllImport("kernel32.dll")> _
            Public Shared Function DeleteAtom(ByVal atom As UInt16) As UInt16
            End Function
#End If 'Codemarkers_IncludeAppEnum                     

            <DllImport("kernel32.dll", CharSet:=CharSet.Unicode)>
            Public Shared Function GetModuleHandle(ByVal lpString As String) As IntPtr
            End Function

        End Class 'NativeMethods

        ' ********************* End of imported Win32 functions *********************

        ' Atom name. This ATOM will be set by the host application when code markers are enabled
        ' in the registry.
        Private Const AtomName As String = "VSCodeMarkersEnabled"

        ' Test CodeMarkers DLL name
        Private Const TestDllName As String = "Microsoft.Internal.Performance.CodeMarkers.dll"

        ' Product CodeMarkers DLL name
        Private Const ProductDllName As String = "Microsoft.VisualStudio.CodeMarkers.dll"

        ' Do we want to use code markers?
        Private fUseCodeMarkers As Boolean

        ' should CodeMarker events be fired to the test or product CodeMarker DLL
        Private fShouldUseTestDll As Boolean?
        Private regroot As String
        Private ReadOnly Property ShouldUseTestDll As Boolean
            Get
                If Not fShouldUseTestDll.HasValue Then

                    Try
                        ' this code can either be used in an InitPerf (loads CodeMarker DLL) or AttachPerf context (CodeMarker DLL already loaded)
                        ' in the InitPerf context we have a regroot and should check for the test DLL registration
                        ' in the AttachPerf context we should see which module is already loaded 
                        If regroot = Nothing Then
                            fShouldUseTestDll = NativeMethods.GetModuleHandle(ProductDllName) = IntPtr.Zero
                        Else
                            ' if CodeMarkers are explictly enabled in the registry then try to
                            ' use the test DLL, otherwise fall back to trying to use the product DLL
                            fShouldUseTestDll = CodeMarkers.UsePrivateCodeMarkers(regroot)
                        End If
                    Catch ex As Exception
                        fShouldUseTestDll = True
                    End Try
                End If

                Return fShouldUseTestDll.Value
            End Get
        End Property

        ' Constructor. Do not call directly. Use CodeMarkers.Instance to access the singleton
        ' Checks to see if code markers are enabled by looking for a named ATOM
        Private Sub New()
            ' This ATOM will be set by the native Code Markers host
            fUseCodeMarkers = (NativeMethods.FindAtom(AtomName) <> 0)
        End Sub 'New

        ' Implements sending the code marker value nTimerID.
        ' Implements sending the code marker value nTimerID.
        Public Sub CodeMarker(ByVal nTimerID As Integer)
            If Not fUseCodeMarkers Then
                Return
            End If
            Try
                If ShouldUseTestDll Then
                    NativeMethods.TestDllPerfCodeMarker(nTimerID, 0, 0)
                Else
                    NativeMethods.ProductDllPerfCodeMarker(nTimerID, 0, 0)
                End If
            Catch ex As DllNotFoundException
                ' If the DLL doesn't load or the entry point doesn't exist, then
                ' abandon all further attempts to send codemarkers.
                fUseCodeMarkers = False
            End Try
        End Sub 'CodeMarker

        ' Checks the registry to see if code markers are enabled
        Private Shared Function UsePrivateCodeMarkers(ByVal strRegRoot As String) As Boolean

            ' SECURITY: We no longer check HKCU because that might lead to a DLL spoofing attack via
            ' the code markers DLL. Check only HKLM since that has a strong ACL. You therefore need
            ' admin rights to enable/disable code markers.

            ' It doesn't matter what the string says, if it's present and not empty, code markers are enabled
            Return Not String.IsNullOrEmpty(GetPerformanceSubKey(Registry.CurrentUser, strRegRoot))
        End Function 'UseCodeMarkers

        ' Reads the Performance subkey from the appropriate registry key
        ' Returns: the Default value from the subkey (null if not found)
        Private Shared Function GetPerformanceSubKey(ByVal hKey As RegistryKey, ByVal strRegRoot As String) As String
            If hKey Is Nothing Then
                Return Nothing
            End If

            ' does the subkey exist
            Dim str As String = Nothing
            Using key As RegistryKey = hKey.OpenSubKey(strRegRoot & "\Performance")
                If Not key Is Nothing Then
                    ' reads the default value
                    str = key.GetValue("").ToString()
                End If
            End Using
            Return str
        End Function 'SubKeyExist

#If Codemarkers_IncludeAppEnum Then
        ' Check the registry and, if appropriate, loads and initializes the code markers dll.
        ' Must be used only if your code is called from outside of VS.
        Public Sub InitPerformanceDll(ByVal iApp As Integer, ByVal strRegRoot As String)
            If strRegRoot = Nothing Then
                Throw New ArgumentNullException("regRoot")
            End If
            Try
                regroot = strRegRoot
                If ShouldUseTestDll Then
                    NativeMethods.TestDllInitPerf(iApp)
                Else
                    NativeMethods.ProductDllInitPerf(iApp)
                End If                
                fUseCodeMarkers = True
                NativeMethods.AddAtom(AtomName)                
            Catch ex As BadImageFormatException
                fUseCodeMarkers = False
            Catch ex As DllNotFoundException
                fUseCodeMarkers = False
            End Try          
        End Sub 'InitPerformanceDll

        ' Opposite of InitPerformanceDLL. Call it when your app does not need the code markers dll.
        Public Sub UninitializePerformanceDLL(ByVal iApp As Integer)

            Dim fUsingTestDll as Boolean? = fShouldUseTestDll ' reset which DLL we should use (needed for unit testing)
            fShouldUseTestDll = null ' reset which DLL we should use (needed for unit testing)
            regroot = null

            If Not fUseCodeMarkers Then
                Return
            End If

            fUseCodeMarkers = False

            Dim atom As UInt16 = NativeMethods.FindAtom(AtomName)
            If atom <> 0 Then
                NativeMethods.DeleteAtom(atom)
            End If
            Try
                If fUsingTestDll.HasValue Then  ' If it doesn't have a value, then we've never initialized the DLL.
                    If fUsingTestDll.Value Then
                        NativeMethods.TestDllUnInitPerf(iApp)
                    Else
                        NativeMethods.ProductDllUnInitPerf(iApp)
                    End If                  
                End If
            Catch ex As DllNotFoundException
                ' Swallow the exception
            End Try
        End Sub 'UninitializePerformanceDLL
#End If 'Codemarkers_IncludeAppEnum

    End Class 'ManagedCodeMarkers

#If Not Codemarkers_NoCodeMarkerStartEnd Then
    ''' <summary>
    ''' Use CodeMarkerStartEnd in a using clause when you need to bracket an
    ''' operation with a start/end CodeMarker event pair.
    ''' </summary>
    Friend Structure CodeMarkerStartEnd
        Implements IDisposable

        Private ReadOnly _endCodeMarker As Integer

        Public Sub New(startCodeMarker As Integer, endCodeMarker As Integer)
            Debug.Assert(endCodeMarker <> 0)
            CodeMarkers.Instance.CodeMarker(startCodeMarker)
            _endCodeMarker = endCodeMarker
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            CodeMarkers.Instance.CodeMarker(_endCodeMarker)
        End Sub
    End Structure
#End If

    Enum CodeMarkerEvent

        perfMSVSEditorsShowTabBegin = 8400
        perfMSVSEditorsShowTabEnd = 8401
        perfMSVSEditorsActivateLogicalViewStart = 8402
        perfMSVSEditorsActivateLogicalViewEnd = 8403

        perfMSVSEditorsReferencePageWCFAdded = 8413
        perfMSVSEditorsReferencePagePostponedUIRefreshDone = 8414
    End Enum

End Namespace
