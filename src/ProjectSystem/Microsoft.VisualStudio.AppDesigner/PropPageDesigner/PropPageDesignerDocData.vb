Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.TextManager.Interop
Imports win = Microsoft.VisualStudio.Editors.AppDesInterop.win
Imports Microsoft.VisualStudio.Editors.AppDesInterop

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' The DocData for the application Designer.
    ''' </summary>
    ''' <remarks>
    ''' The Application Designer does not have a physical file for persistance 
    ''' since it uses the project system directly.  We use this to prevent VS looking for a file
    '''</remarks>
    <ComSourceInterfaces(GetType(IVsTextBufferDataEvents))> _
    Public NotInheritable Class PropPageDesignerDocData
        Implements IDisposable
        Implements IVsUserData
        Implements IVsPersistDocData2
        Implements OLE.Interop.IObjectWithSite
        Implements IVsTextBufferProvider

        'Event support for IVsTextBufferDataEvents
        Public Delegate Sub LoadCompletedDelegate(ByVal Reload As Integer)
        Public Delegate Sub FileChangedDelegate(ByVal ChangeFlags As UInteger, ByVal FileAttrs As UInteger)

        Public Event OnLoadCompleted As LoadCompletedDelegate
        Public Event OnFileChanged As FileChangedDelegate

        ' VsTextBuffer class used for providing a textbuffer implementation 
        ' which the IDE needs to operate.  Nothing currently written or read from the 
        ' text stream.
        Private m_VsTextBuffer As IVsTextBuffer

        'Service provider members
        Private m_BaseProvider As IServiceProvider
        Private m_SiteProvider As IServiceProvider

        ' IVsHierarchy, ItemId, and cookie passed in on registration
        Private m_VsHierarchy As IVsHierarchy
        Private m_ItemId As UInteger
        Private m_DocCookie As UInteger
        Private m_MkDocument As String

        ' Dirty and readonly state
        Private m_IsReadOnly As Boolean
        Private m_IsDirty As Boolean

        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="BaseProvider"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal BaseProvider As IServiceProvider)
            'not must init to do here
            m_BaseProvider = BaseProvider
        End Sub

        ''' <summary>
        ''' Creates VsTextBuffer if necessary and returns the instance of VsTextBuffer
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Private ReadOnly Property VsTextStream() As IVsTextBuffer
            Get
                If m_VsTextBuffer IsNot Nothing Then
                    Return m_VsTextBuffer
                End If

                ' Get the LocalRegistry service and use it to create an instance of the VsTextBuffer class
                Dim localRegistry As ILocalRegistry = Nothing
                If m_BaseProvider IsNot Nothing Then
                    localRegistry = DirectCast(m_BaseProvider.GetService(GetType(ILocalRegistry)), ILocalRegistry)
                End If
                If localRegistry Is Nothing Then
                    Throw New COMException(SR.GetString(SR.DFX_NoLocalRegistry), AppDesInterop.NativeMethods.E_FAIL)
                End If

                'CONSIDER: Need to check with FX team about removing assert in MS.VS.Shell.Design.DesignerWindowPane.RegisterView
                ' If we don't provide VsTextBuffer, we assert over and over again 
                Try
                    Dim guidTemp As Guid = GetType(IVsTextStream).GUID
                    Dim objPtr As IntPtr = IntPtr.Zero
                    VSErrorHandler.ThrowOnFailure(localRegistry.CreateInstance(GetType(VsTextBufferClass).GUID, Nothing, guidTemp, win.CLSCTX_INPROC_SERVER, objPtr))

                    If Not objPtr.Equals(IntPtr.Zero) Then
                        m_VsTextBuffer = CType(Marshal.GetObjectForIUnknown(objPtr), IVsTextStream)
                        Marshal.Release(objPtr)

                        Dim ows As Microsoft.VisualStudio.OLE.Interop.IObjectWithSite = TryCast(m_VsTextBuffer, Microsoft.VisualStudio.OLE.Interop.IObjectWithSite)
                        If (ows IsNot Nothing) Then
                            Dim sp As Microsoft.VisualStudio.OLE.Interop.IServiceProvider = TryCast(m_BaseProvider.GetService(GetType(Microsoft.VisualStudio.OLE.Interop.IServiceProvider)), Microsoft.VisualStudio.OLE.Interop.IServiceProvider)
                            Debug.Assert(sp IsNot Nothing, "Expected to get a native service provider from our managed service provider")

                            If (sp IsNot Nothing) Then
                                ows.SetSite(sp)
                            End If
                        End If
                    End If
                Catch ex As Exception
                    Throw New COMException(SR.GetString(SR.DFX_UnableCreateTextBuffer), AppDesInterop.NativeMethods.E_FAIL)
                End Try
                Return m_VsTextBuffer
            End Get
        End Property

#Region "IVsUserData"
        ''' <summary>
        ''' Gets docdata specific data based on guid
        ''' </summary>
        ''' <param name="riidKey"></param>
        ''' <param name="pvtData"></param>
        ''' <remarks></remarks>
        Public Function GetData(ByRef riidKey As System.Guid, ByRef pvtData As Object) As Integer Implements TextManager.Interop.IVsUserData.GetData
            If riidKey.Equals(GetType(IVsUserData).GUID) Then
                'IID_IVsUserData (GUID_VsBufferMoniker) is the guid used for retrieving MkDocument (filename)
                Return NativeMethods.S_OK
                pvtData = m_MkDocument
                Return NativeMethods.S_OK
            ElseIf m_VsTextBuffer IsNot Nothing Then
                Return CType(m_VsTextBuffer, IVsUserData).GetData(riidKey, pvtData)
            Else
                Return NativeMethods.E_FAIL
            End If
        End Function

        ''' <summary>
        ''' Sets docdata specific data using guid key
        ''' </summary>
        ''' <param name="riidKey"></param>
        ''' <param name="vtData"></param>
        ''' <remarks></remarks>
        Public Function SetData(ByRef riidKey As System.Guid, ByVal vtData As Object) As Integer Implements TextManager.Interop.IVsUserData.SetData
            If m_VsTextBuffer IsNot Nothing Then
                Return CType(m_VsTextBuffer, IVsUserData).SetData(riidKey, vtData)
            Else
                Return NativeMethods.E_FAIL
            End If
        End Function
#End Region

#Region "IVsPersistDocData2 implementation"
#Region "IVsPersistDocData implementation"
        'The IVsPersistDocData2 inherits from IVsPersistDocData
        'The compiler expects both interfaces to be implemented
        'Whether this is a bug or not has yet to be determined
        'It may having something to do with it being defined in an interop assembly.

        Public Function Close() As Integer Implements Shell.Interop.IVsPersistDocData.Close
            Return Me.Close2()
        End Function

        Public Function GetGuidEditorType(ByRef pClassID As System.Guid) As Integer Implements Shell.Interop.IVsPersistDocData.GetGuidEditorType
            Return Me.GetGuidEditorType2(pClassID)
        End Function

        Public Function IsDocDataDirty(ByRef pfDirty As Integer) As Integer Implements Shell.Interop.IVsPersistDocData.IsDocDataDirty
            Return Me.IsDocDataDirty2(pfDirty)
        End Function

        Public Function IsDocDataReloadable(ByRef pfReloadable As Integer) As Integer Implements Shell.Interop.IVsPersistDocData.IsDocDataReloadable
            Return Me.IsDocDataReloadable2(pfReloadable)
        End Function

        Public Function LoadDocData(ByVal pszMkDocument As String) As Integer Implements Shell.Interop.IVsPersistDocData.LoadDocData
            Return Me.LoadDocData2(pszMkDocument)
        End Function

        Public Function OnRegisterDocData(ByVal docCookie As UInteger, ByVal pHierNew As Shell.Interop.IVsHierarchy, ByVal itemidNew As UInteger) As Integer Implements Shell.Interop.IVsPersistDocData.OnRegisterDocData
            Return Me.OnRegisterDocData2(docCookie, pHierNew, itemidNew)
        End Function

        Public Function ReloadDocData(ByVal grfFlags As UInteger) As Integer Implements Shell.Interop.IVsPersistDocData.ReloadDocData
            Return Me.ReloadDocData2(grfFlags)
        End Function

        Public Function RenameDocData(ByVal grfAttribs As UInteger, ByVal pHierNew As Shell.Interop.IVsHierarchy, ByVal itemidNew As UInteger, ByVal pszMkDocumentNew As String) As Integer Implements Shell.Interop.IVsPersistDocData.RenameDocData
            Return Me.RenameDocData2(grfAttribs, pHierNew, itemidNew, pszMkDocumentNew)
        End Function

        Public Function SaveDocData(ByVal dwSave As Shell.Interop.VSSAVEFLAGS, ByRef pbstrMkDocumentNew As String, ByRef pfSaveCanceled As Integer) As Integer Implements Shell.Interop.IVsPersistDocData.SaveDocData
            Return Me.SaveDocData2(dwSave, pbstrMkDocumentNew, pfSaveCanceled)
        End Function

        Public Function SetUntitledDocPath(ByVal pszDocDataPath As String) As Integer Implements Shell.Interop.IVsPersistDocData.SetUntitledDocPath
            Return Me.SetUntitledDocPath2(pszDocDataPath)
        End Function
#End Region
        Public Function Close2() As Integer Implements Shell.Interop.IVsPersistDocData2.Close
            'Nothing to do here, no real file to close
            Return NativeMethods.S_OK
        End Function

        Public Function GetGuidEditorType2(ByRef pClassID As System.Guid) As Integer Implements Shell.Interop.IVsPersistDocData2.GetGuidEditorType
            pClassID = PropPageDesignerEditorFactory.EditorGuid
        End Function

        Public Function IsDocDataDirty2(ByRef pfDirty As Integer) As Integer Implements Shell.Interop.IVsPersistDocData2.IsDocDataDirty
            If m_IsDirty Then
                pfDirty = 1
            Else
                pfDirty = 0
            End If
        End Function

        Public Function IsDocDataReadOnly(ByRef pfReadOnly As Integer) As Integer Implements Shell.Interop.IVsPersistDocData2.IsDocDataReadOnly
            If m_IsReadOnly Then
                pfReadOnly = 1
            Else
                pfReadOnly = 0
            End If
        End Function

        Public Function IsDocDataReloadable2(ByRef pfReloadable As Integer) As Integer Implements Shell.Interop.IVsPersistDocData2.IsDocDataReloadable
            pfReloadable = 0
        End Function

        Public Function LoadDocData2(ByVal pszMkDocument As String) As Integer Implements Shell.Interop.IVsPersistDocData2.LoadDocData
            'Nothing to do here, no real file to load
            m_MkDocument = pszMkDocument
            RaiseEvent OnLoadCompleted(0) 'FALSE == 0
        End Function

        Public Function OnRegisterDocData2(ByVal docCookie As UInteger, ByVal pHierNew As Shell.Interop.IVsHierarchy, ByVal itemidNew As UInteger) As Integer Implements Shell.Interop.IVsPersistDocData2.OnRegisterDocData
            m_DocCookie = docCookie
            m_VsHierarchy = pHierNew
            m_ItemId = itemidNew
        End Function

        Public Function ReloadDocData2(ByVal grfFlags As UInteger) As Integer Implements Shell.Interop.IVsPersistDocData2.ReloadDocData
            'Should we reload anything?
            RaiseEvent OnLoadCompleted(1) 'TRUE == 1
        End Function

        Public Function RenameDocData2(ByVal grfAttribs As UInteger, ByVal pHierNew As Shell.Interop.IVsHierarchy, ByVal itemidNew As UInteger, ByVal pszMkDocumentNew As String) As Integer Implements Shell.Interop.IVsPersistDocData2.RenameDocData
            Return VSConstants.E_NOTIMPL
        End Function

        Public Function SaveDocData2(ByVal dwSave As Shell.Interop.VSSAVEFLAGS, ByRef pbstrMkDocumentNew As String, ByRef pfSaveCanceled As Integer) As Integer Implements Shell.Interop.IVsPersistDocData2.SaveDocData
            pfSaveCanceled = 0
            'Nothing to do since we have no true file backing
        End Function

        Public Function SetDocDataDirty(ByVal fDirty As Integer) As Integer Implements Shell.Interop.IVsPersistDocData2.SetDocDataDirty
            If fDirty <> 0 Then
                m_IsDirty = True
            Else
                m_IsDirty = False
            End If
        End Function

        Public Function SetDocDataReadOnly(ByVal fReadOnly As Integer) As Integer Implements Shell.Interop.IVsPersistDocData2.SetDocDataReadOnly
            If fReadOnly <> 0 Then
                m_IsReadOnly = True
            Else
                m_IsReadOnly = False
            End If
            Return VSConstants.S_OK
        End Function

        Public Function SetUntitledDocPath2(ByVal pszDocDataPath As String) As Integer Implements Shell.Interop.IVsPersistDocData2.SetUntitledDocPath
            Return VSConstants.E_NOTIMPL
        End Function
#End Region

#Region "IVsTextBufferProvider"
        ''' <summary>
        ''' Returns the IVsTextLines for our virtual buffer
        ''' </summary>
        ''' <param name="ppTextBuffer"></param>
        ''' <remarks></remarks>
        Public Function GetTextBuffer(ByRef ppTextBuffer As TextManager.Interop.IVsTextLines) As Integer Implements Shell.Interop.IVsTextBufferProvider.GetTextBuffer
            If TypeOf VsTextStream Is IVsTextLines Then
                ppTextBuffer = CType(VsTextStream, IVsTextLines)
            Else
                ppTextBuffer = Nothing
            End If
        End Function

        ''' <summary>
        ''' Locks/Unlocks our buffer
        ''' </summary>
        ''' <param name="fLock"></param>
        ''' <remarks></remarks>
        Public Function LockTextBuffer(ByVal fLock As Integer) As Integer Implements Shell.Interop.IVsTextBufferProvider.LockTextBuffer
            If fLock = 0 Then
                Return VsTextStream.UnlockBuffer()
            Else
                Return VsTextStream.LockBuffer()
            End If
        End Function

        ''' <summary>
        ''' SetTextBuffer is currently unsupported.
        ''' </summary>
        ''' <param name="pTextBuffer"></param>
        ''' <remarks></remarks>
        Public Function SetTextBuffer(ByVal pTextBuffer As TextManager.Interop.IVsTextLines) As Integer Implements Shell.Interop.IVsTextBufferProvider.SetTextBuffer
            Debug.Fail("SetTextBuffer not supported in Application Designer!")
        End Function
#End Region

#Region "OLE.Interop.IObjectWithSite"
        ''' <summary>
        ''' Returns the current hosting site for the DocData
        ''' </summary>
        ''' <param name="riid"></param>
        ''' <param name="ppvSite"></param>
        ''' <remarks></remarks>
        Public Sub GetSite(ByRef riid As System.Guid, ByRef ppvSite As System.IntPtr) Implements OLE.Interop.IObjectWithSite.GetSite
            Dim punk As IntPtr = Marshal.GetIUnknownForObject(m_SiteProvider)
            Dim hr As Integer
            hr = Marshal.QueryInterface(punk, riid, ppvSite)
            Marshal.Release(punk)
            If AppDesInterop.NativeMethods.Failed(hr) Then
                Marshal.ThrowExceptionForHR(hr)
            End If
        End Sub

        ''' <summary>
        ''' Sets the hosting site for the DocData
        ''' </summary>
        ''' <param name="pUnkSite"></param>
        ''' <remarks></remarks>
        Public Sub SetSite(ByVal pUnkSite As Object) Implements OLE.Interop.IObjectWithSite.SetSite
            If TypeOf pUnkSite Is OLE.Interop.IServiceProvider Then
                m_SiteProvider = New Shell.ServiceProvider(DirectCast(pUnkSite, OLE.Interop.IServiceProvider))
            Else
                m_SiteProvider = Nothing
            End If
        End Sub
#End Region

#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Disposes of any the doc data
        ''' </summary>
        ''' <remarks></remarks>
        Public Overloads Sub Dispose() Implements System.IDisposable.Dispose
            Dispose(True)
        End Sub

        ''' <summary>
        ''' Disposes of contained objects
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overloads Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                ' Dispose managed resources.
                m_BaseProvider = Nothing
                If m_VsTextBuffer IsNot Nothing Then
                    ' Close IVsPersistDocData
                    Dim docData As IVsPersistDocData = TryCast(m_VsTextBuffer, IVsPersistDocData)
                    If docData IsNot Nothing Then
                        docData.Close()
                    End If
                    m_VsTextBuffer = Nothing
                End If
                m_VsHierarchy = Nothing
            End If
            ' Call the appropriate methods to clean up 
            ' unmanaged resources here.
            ' If disposing is false, 
            ' only the following code is executed.

        End Sub
#End Region

    End Class

End Namespace

